using System;
namespace VintageCollector.Media
{
    public struct PngDimensions { public int Width, Height; }
    public static class MediaPng
    {
        static void Require(bool value) { if(!value) throw new System.IO.InvalidDataException("Invalid or unsupported bounded PNG"); }
        static uint Big(byte[] b,int p) { return (uint)b[p]<<24 | (uint)b[p+1]<<16 | (uint)b[p+2]<<8 | b[p+3]; }
        static uint Crc(byte[] b,int start,int length)
        {
            uint crc=0xffffffff;
            for(int i=start;i<start+length;i++) { crc^=b[i]; for(int k=0;k<8;k++) crc=(crc>>1)^((crc&1)!=0?0xedb88320u:0); }
            return crc^0xffffffff;
        }
        public static PngDimensions Inspect(byte[] bytes, bool cardRatio)
        {
            Require(bytes!=null && bytes.Length>=57 && bytes.Length<=MediaFiles.MediaByteLimit);
            byte[] signature={137,80,78,71,13,10,26,10};
            for(int i=0;i<8;i++) Require(bytes[i]==signature[i]);
            // IHDR/dimension gates precede any decoder invocation or pixel allocation.
            Require(Big(bytes,8)==13 && System.Text.Encoding.ASCII.GetString(bytes,12,4)=="IHDR");
            uint w=Big(bytes,16), h=Big(bytes,20);
            Require(w>0 && h>0 && w<=4096 && h<=4096 && (ulong)w*h<=8388608);
            Require(bytes[24]==8 && (bytes[25]==2 || bytes[25]==6) && bytes[26]==0 && bytes[27]==0 && bytes[28]==0);
            if(cardRatio) Require(Math.Abs(w/(double)h-63.0/88.0)<=0.025);
            bool data=false, end=false, afterData=false;
            for(int p=8;p<bytes.Length;) {
                Require(p<=bytes.Length-12);
                uint n=Big(bytes,p); Require(n<=bytes.Length-p-12);
                int size=(int)n;
                string tag=System.Text.Encoding.ASCII.GetString(bytes,p+4,4);
                Require(Crc(bytes,p+4,size+4)==Big(bytes,p+8+size));
                if(p==8) Require(tag=="IHDR" && size==13);
                else if(tag=="IDAT") { Require(!afterData); data=true; }
                else if(tag=="IEND") { Require(data && size==0 && p+12==bytes.Length); end=true; }
                else {
                    // Only RGB/RGBA subset; unknown critical chunks (including palette) fail closed.
                    Require(tag.Length==4 && tag[0]>='a' && tag[0]<='z');
                    if(data) afterData=true;
                }
                p+=size+12;
            }
            Require(data && end);
            return new PngDimensions { Width=(int)w, Height=(int)h };
        }
    }
}
