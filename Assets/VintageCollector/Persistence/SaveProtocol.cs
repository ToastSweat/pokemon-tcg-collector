using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using VintageCollector.Domain;
namespace VintageCollector.Persistence
{
    // No delete-then-move fallback: inability to perform atomic replacement blocks the action.
    internal sealed class SaveProtocol
    {
        const string Header="VTCG-SAVE-1\n";
        const int MaximumBytes=32*1024*1024;
        readonly string directory; readonly IStateSerializer serializer; readonly Catalog catalog; readonly ISaveFaultInjector faults;
        internal SaveProtocol(string directory,IStateSerializer serializer,Catalog catalog,ISaveFaultInjector faults) { this.directory=directory; this.serializer=serializer; this.catalog=catalog; this.faults=faults; }
        string PathFor(string name) { return Path.Combine(directory,name); }
        static bool Exists(string path) { try { File.GetAttributes(path); return true; } catch(FileNotFoundException) { return false; } catch(DirectoryNotFoundException) { return false; } }
        void At(string stage) { if(faults!=null) faults.At(stage); }
        static string Digest(string payload) { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(new UTF8Encoding(false,true).GetBytes(payload))).Replace("-","").ToLowerInvariant(); }
        static string Envelope(string payload) { return Header+Digest(payload)+"\n"+payload; }
        internal static bool Equal(SessionData a,SessionData b)
        {
            if(a.schemaVersion!=b.schemaVersion || a.revision!=b.revision || a.packs.Length!=b.packs.Length) return false;
            for(int i=0;i<a.packs.Length;i++) {
                var x=a.packs[i]; var y=b.packs[i];
                if(!StateCopy.SameIdentity(x,y) || x.wrapperOpened!=y.wrapperOpened || x.revealedCount!=y.revealedCount) return false;
                for(int j=0;j<x.newToVirtual.Length;j++) if(x.newToVirtual[j]!=y.newToVirtual[j]) return false;
            }
            return true;
        }
        static string Payload(string text)
        {
            if(text!=null && text.StartsWith("VTCG-SAVE-",StringComparison.Ordinal) && !text.StartsWith(Header,StringComparison.Ordinal)) throw new InvalidDataException("Unsupported save envelope version; bytes preserved");
            if(text==null || !text.StartsWith(Header,StringComparison.Ordinal) || text.Length<Header.Length+65 || text[Header.Length+64]!='\n') throw new EnvelopeCorruption("Invalid save envelope");
            string checksum=text.Substring(Header.Length,64), payload=text.Substring(Header.Length+65);
            if(checksum!=Digest(payload)) throw new EnvelopeCorruption("Save checksum mismatch");
            return payload;
        }
        SessionData Decode(string text)
        {
            string payload=Payload(text);
            SessionData data;
            try { data=serializer.Deserialize(payload); } catch(Exception e) { throw new InvalidDataException("Save payload cannot be deserialized; bytes preserved",e); }
            StateValidation.Session(data,catalog); return StateCopy.Session(data);
        }
        sealed class EnvelopeCorruption : IOException { internal EnvelopeCorruption(string message):base(message) { } }
        sealed class Copy
        {
            internal string Name, Text; internal SessionData Data; internal bool Corrupt;
        }
        Copy Read(string name)
        {
            string path=PathFor(name); if(!Exists(path)) return null;
            if(new FileInfo(path).Length>MaximumBytes) throw new InvalidDataException("Save exceeds 32 MiB limit; preserved");
            var c=new Copy {Name=name};
            try { c.Text=File.ReadAllText(path,new UTF8Encoding(false,true)); c.Data=Decode(c.Text); if(c.Data.revision==0) throw new InvalidDataException("Unexpected persisted blank session; bytes preserved"); }
            catch(EnvelopeCorruption) { c.Corrupt=true; }
            catch(DecoderFallbackException) { c.Corrupt=true; }
            return c;
        }
        static bool Same(Copy c,SessionData d) { return c!=null && !c.Corrupt && Equal(c.Data,d); }
        static void Compatible(SessionData older,SessionData newer)
        {
            if(older.revision==newer.revision) { if(!Equal(older,newer)) throw new InvalidDataException("Conflicting saves at same revision"); return; }
            if(older.revision>newer.revision) { Compatible(newer,older); return; }
            if(older.packs.Length>newer.packs.Length) throw new InvalidDataException("Archive shrank across revisions");
            for(int i=0;i<older.packs.Length;i++) {
                var a=older.packs[i]; var b=newer.packs[i];
                if(!StateCopy.SameIdentity(a,b) || (a.wrapperOpened && !b.wrapperOpened) || a.revealedCount>b.revealedCount) throw new InvalidDataException("Save lineage conflict");
                for(int j=0;j<a.newToVirtual.Length;j++) if(a.newToVirtual[j]!=b.newToVirtual[j]) throw new InvalidDataException("Award lineage conflict");
            }
        }
        void Flush(string name,string text)
        {
            byte[] bytes=new UTF8Encoding(false,true).GetBytes(text);
            using(var file=new FileStream(PathFor(name),FileMode.Create,FileAccess.Write,FileShare.None)) { file.Write(bytes,0,bytes.Length); file.Flush(true); }
        }
        void Replace(string temporary,string destination,string backup=null)
        {
            if(Exists(PathFor(destination))) File.Replace(PathFor(temporary),PathFor(destination),backup==null?null:PathFor(backup));
            else File.Move(PathFor(temporary),PathFor(destination));
        }
        void Preserve(Copy c) { if(c!=null && c.Corrupt) File.Copy(PathFor(c.Name),PathFor(c.Name+".corrupt-"+Guid.NewGuid().ToString("N")),false); }
        void VerifyPair(SessionData expected)
        {
            if(!Same(Read("state.save"),expected) || !Same(Read("state.backup"),expected)) throw new IOException("Save read-back did not match the durable candidate");
        }
        sealed class Anchor { internal long Revision; internal string Hash; }
        Anchor ReadAnchor()
        {
            if(!Exists(PathFor("state.anchor"))) return null;
            try {
                if(new FileInfo(PathFor("state.anchor")).Length>256) throw new InvalidDataException("Oversized save anchor");
                string[] fields=Payload(File.ReadAllText(PathFor("state.anchor"),new UTF8Encoding(false,true))).Split('\n');
                long revision;
                if(fields.Length!=2 || !long.TryParse(fields[0],System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out revision) || revision<1 || fields[0]!=revision.ToString(System.Globalization.CultureInfo.InvariantCulture) || fields[1].Length!=64) throw new InvalidDataException("Invalid save anchor");
                foreach(char c in fields[1]) if(!((c>='0' && c<='9') || (c>='a' && c<='f'))) throw new InvalidDataException("Invalid anchor digest");
                return new Anchor {Revision=revision,Hash=fields[1]};
            } catch(EnvelopeCorruption e) { throw new InvalidDataException("Save anchor corrupt; all data preserved",e); }
            catch(DecoderFallbackException e) { throw new InvalidDataException("Save anchor encoding corrupt; all data preserved",e); }
        }
        void WriteAnchor(SessionData data,string text,bool inject)
        {
            if(inject) At("before-anchor");
            Flush("state.anchor-writing",Envelope(data.revision.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n"+Digest(text)));
            if(inject) At("after-anchor-flush"); Replace("state.anchor-writing","state.anchor"); if(inject) At("after-anchor-replace");
            var anchor=ReadAnchor(); if(anchor==null || anchor.Revision!=data.revision || anchor.Hash!=Digest(text)) throw new IOException("Anchor read-back mismatch");
        }
        internal SessionData Load(SessionData known,out string notice)
        {
            notice=null;
            // Validate all authoritative copies before modifying any. Unsupported/semantic data never falls back silently.
            Anchor anchor=ReadAnchor();
            Copy primary=Read("state.save"), backup=Read("state.backup"), pending=Read("state.pending");
            if(pending!=null && pending.Corrupt) throw new InvalidDataException("Prepared save is corrupt; preserved for manual recovery");
            var valid=new List<Copy>(); foreach(var c in new[]{primary,backup,pending}) if(c!=null && !c.Corrupt) valid.Add(c);
            if(valid.Count==0) {
                if(primary!=null || backup!=null || pending!=null || anchor!=null || (known!=null && known.revision>0)) throw new InvalidDataException("No valid recovery copy; all bytes preserved");
                if(Exists(PathFor("state.writing"))) notice="Unprepared interrupted write retained; no pack had been committed.";
                return new SessionData {schemaVersion=1,revision=0,packs=new PackRecordData[0]};
            }
            Copy chosen=valid[0];
            foreach(var c in valid) { Compatible(c.Data,chosen.Data); if(c.Data.revision>chosen.Data.revision) chosen=c; }
            if(known!=null) { if(chosen.Data.revision<known.revision) throw new InvalidDataException("Recovery would roll back this live session; bytes preserved"); Compatible(known,chosen.Data); }
            if(anchor!=null && (chosen.Data.revision<anchor.Revision || (chosen.Data.revision==anchor.Revision && Digest(chosen.Text)!=anchor.Hash))) throw new InvalidDataException("Recovery would roll back or conflict with acknowledged state; all copies preserved");
            if(!Same(primary,chosen.Data) || !Same(backup,chosen.Data) || pending!=null || anchor==null || anchor.Revision!=chosen.Data.revision) {
                Preserve(primary); Preserve(backup);
                if(!Same(primary,chosen.Data)) { Flush("state.installing",chosen.Text); Replace("state.installing","state.save"); }
                if(!Same(backup,chosen.Data)) { Flush("state.backup-writing",chosen.Text); Replace("state.backup-writing","state.backup"); }
                VerifyPair(chosen.Data); WriteAnchor(chosen.Data,chosen.Text,false);
                if(pending!=null) File.Delete(PathFor("state.pending"));
                notice="Recovered durable revision "+chosen.Data.revision+" from "+chosen.Name+"; no cards rerolled. Corrupt copies, if any, were preserved.";
            }
            return StateCopy.Session(chosen.Data);
        }
        internal void Save(SessionData candidate)
        {
            StateValidation.Session(candidate,catalog);
            string text=Envelope(serializer.Serialize(StateCopy.Session(candidate)));
            if(new UTF8Encoding(false,true).GetByteCount(text)>MaximumBytes) throw new InvalidDataException("Save exceeds 32 MiB limit");
            if(!Equal(Decode(text),candidate)) throw new InvalidDataException("Serializer changed the candidate; no write performed");
            At("before-temp-write");
            byte[] bytes=new UTF8Encoding(false,true).GetBytes(text);
            using(var file=new FileStream(PathFor("state.writing"),FileMode.Create,FileAccess.Write,FileShare.None)) {
                file.Write(bytes,0,bytes.Length); At("after-temp-write"); file.Flush(true); At("after-temp-flush");
            }
            At("before-prepare"); File.Move(PathFor("state.writing"),PathFor("state.pending")); At("after-prepare");
            Flush("state.installing",text); At("before-replace"); Replace("state.installing","state.save","state.backup"); At("after-replace");
            At("before-backup"); Flush("state.backup-writing",text); At("after-backup-flush"); Replace("state.backup-writing","state.backup"); At("after-backup-replace");
            VerifyPair(candidate); WriteAnchor(candidate,text,true);
            At("before-pending-delete"); File.Delete(PathFor("state.pending")); At("after-pending-delete");
            At("before-ack");
        }
    }
}
