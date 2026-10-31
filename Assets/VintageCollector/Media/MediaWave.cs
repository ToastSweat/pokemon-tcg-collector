using System;
namespace VintageCollector.Media
{
    public sealed class DecodedWave
    {
        public int SampleRate { get; internal set; }
        public int Channels { get; internal set; }
        public float[] Samples { get; internal set; }
    }
    public static class MediaWave
    {
        static bool Tag(byte[] b, int p, string tag) { return p >= 0 && p + 4 <= b.Length && b[p] == tag[0] && b[p+1] == tag[1] && b[p+2] == tag[2] && b[p+3] == tag[3]; }
        static int U16(byte[] b, int p) { return b[p] | b[p+1] << 8; }
        static uint U32(byte[] b, int p) { return (uint)(b[p] | b[p+1] << 8 | b[p+2] << 16 | b[p+3] << 24); }
        static void Require(bool v) { if (!v) throw new System.IO.InvalidDataException("Unsupported or invalid bounded PCM WAV"); }
        public static DecodedWave Decode(byte[] bytes)
        {
            Require(bytes != null && bytes.Length >= 44 && bytes.Length <= 16 * 1024 * 1024);
            Require(Tag(bytes,0,"RIFF") && Tag(bytes,8,"WAVE") && U32(bytes,4) == bytes.Length - 8);
            int fmt=-1, data=-1, length=0;
            for(int p=12;p<bytes.Length;) {
                Require(p <= bytes.Length-8);
                uint size=U32(bytes,p+4); Require(size<=bytes.Length-p-8);
                int start=p+8, n=(int)size;
                if(Tag(bytes,p,"fmt ")) { Require(fmt<0 && n==16); fmt=start; }
                if(Tag(bytes,p,"data")) { Require(data<0); data=start; length=n; }
                long next=(long)start+n+(n&1); Require(next<=bytes.Length); p=(int)next;
            }
            Require(fmt>=0 && data>=0 && length>0);
            int channels=U16(bytes,fmt+2), bits=U16(bytes,fmt+14), align=U16(bytes,fmt+12);
            uint rate=U32(bytes,fmt+4);
            Require(U16(bytes,fmt)==1 && (channels==1 || channels==2) && (bits==16 || bits==24));
            Require(rate>=8000 && rate<=48000 && align==channels*(bits/8) && U32(bytes,fmt+8)==rate*align);
            Require(length%align==0 && length/align <= rate*5);
            int sampleBytes=bits/8;
            var samples=new float[length/sampleBytes];
            for(int i=0,p=data;i<samples.Length;i++,p+=sampleBytes) {
                int v=bytes[p] | bytes[p+1]<<8;
                if(bits==16) v=(short)v;
                else { v |= bytes[p+2]<<16; if((v&0x800000)!=0) v|=unchecked((int)0xff000000); }
                samples[i]=v/(bits==16?32768f:8388608f);
            }
            return new DecodedWave { SampleRate=(int)rate, Channels=channels, Samples=samples };
        }
    }
}
