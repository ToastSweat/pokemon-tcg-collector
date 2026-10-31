using System;
using System.IO;
using System.Collections.Generic;
namespace VintageCollector.Collections
{
 // Deliberately narrow ZIP dialect: single disk, classic stored/deflated entries,
 // ASCII fixed filenames, no descriptors/extras/comments/preamble/trailing bytes.
 internal static class CollectionZipStructure
 {
  static int U16(byte[] b,int p) {Bounds(b,p,2);return b[p]|b[p+1]<<8;}
  static uint U32(byte[] b,int p) {Bounds(b,p,4);return (uint)(b[p]|b[p+1]<<8|b[p+2]<<16|b[p+3]<<24);}
  static int Size(byte[] b,int p) {uint n=U32(b,p);if(n>int.MaxValue) throw new InvalidDataException("ZIP64 or oversized ZIP field");return (int)n;}
  static void Bounds(byte[] b,int p,int n) {if(p<0 || n<0 || p>b.Length-n) throw new InvalidDataException("Truncated ZIP structure");}
  static void Require(bool b) {if(!b) throw new InvalidDataException("Unsupported or inconsistent ZIP structure");}
  internal static Dictionary<string,uint> Check(byte[] b) {
   int end=b.Length-22;Require(end>=0 && U32(b,end)==0x06054b50 && U16(b,end+4)==0 && U16(b,end+6)==0 && U16(b,end+20)==0);
   int count=U16(b,end+10);Require(count>=3 && count<=9 && U16(b,end+8)==count);
   int cd=Size(b,end+16),cdSize=Size(b,end+12);Require(cd<=end && cdSize==end-cd);
   int cursor=cd,local=0;var crcs=new Dictionary<string,uint>(StringComparer.OrdinalIgnoreCase);
   for(int i=0;i<count;i++) {
    Bounds(b,cursor,46);Require(U32(b,cursor)==0x02014b50);
    int flags=U16(b,cursor+8),method=U16(b,cursor+10),nameLength=U16(b,cursor+28),extra=U16(b,cursor+30),comment=U16(b,cursor+32);
    Require((flags&~0x800)==0 && (method==0||method==8) && nameLength>0 && nameLength<=256 && extra==0 && comment==0 && U16(b,cursor+34)==0);
    int compressed=Size(b,cursor+20),size=Size(b,cursor+24),offset=Size(b,cursor+42);Require(size<=CollectionArchive.MemberCap && offset==local);
    Bounds(b,cursor+46,nameLength);string name="";for(int j=0;j<nameLength;j++) {byte ch=b[cursor+46+j];Require(ch>=32&&ch<127);name+=(char)ch;}
    Require(!crcs.ContainsKey(name));uint crc=U32(b,cursor+16);crcs.Add(name,crc);
    Bounds(b,offset,30);Require(U32(b,offset)==0x04034b50 && U16(b,offset+6)==flags && U16(b,offset+8)==method && U32(b,offset+14)==crc && Size(b,offset+18)==compressed && Size(b,offset+22)==size && U16(b,offset+26)==nameLength && U16(b,offset+28)==0);
    Bounds(b,offset+30,nameLength);for(int j=0;j<nameLength;j++) Require(b[offset+30+j]==b[cursor+46+j]);
    long next=(long)offset+30+nameLength+compressed;Require(next<=cd);local=(int)next;cursor+=46+nameLength;
   }
   Require(local==cd && cursor==end);return crcs;
  }
  internal static uint Crc(byte[] b) {
   uint crc=uint.MaxValue;foreach(byte c in b) {crc^=c;for(int i=0;i<8;i++) crc=(crc&1)!=0?(crc>>1)^0xedb88320u:crc>>1;}return ~crc;
  }
 }
}
