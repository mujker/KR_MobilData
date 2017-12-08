using System;
using System.Text;
using SuperSocket.ProtoBase;

namespace KR_MobilData.Socket
{
    public class HxReceiveFilter : TerminatorReceiveFilter<StringPackageInfo>
    {
        public HxReceiveFilter() : base(Encoding.UTF8.GetBytes("#"))
        {
        }

        public override StringPackageInfo ResolvePackage(IBufferStream bufferStream)
        {
            try
            {
                var rstr = bufferStream.ReadString((int) bufferStream.Length, Encoding.UTF8);
                return new StringPackageInfo(rstr, new BasicStringParser());
            }
            catch (Exception)
            {
                return new StringPackageInfo(string.Empty, new BasicStringParser());
            }
        }
    }
}