using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
namespace bM_.o_
{
    class R_
    {        
        public static R_ h_ = new R_();
        public string this[int hC_] 
        { 
            get 
            { 
                if (IB_ == null)
                {
                    using (var oK_ = new MemoryStream(Convert.FromBase64String(@"AAEAAAD/////AQAAAAAAAAARAQAAAB8AAAAGAgAAAAZNaW5lcjEGAwAAAAVDQ0xjZAYEAAAACE1pbmVyTENEBgUAAAAbLS0tLS1UaHJ1c3RlciBzZXR1cCBpbml0LS0tBgYAAAARVG90YWwgdGhydXN0ZXJzOiAGBwAAAAxUb3RhbCBHeXJvOiAGCAAAAB9ObyB0aHJ1c3RlcnMgb3IgZ3lybywgaW5pdCBGQUlMBgkAAAAiTm8gY29jcGl0IG9yIHJlbW90ZSBjdHIsIGluaXQgRkFJTAYKAAAACFRociB1cDogBgsAAAAKVGhyIGRvd246IAYMAAAAClRociBsZWZ0OiAGDQAAAAtUaHIgcmlnaHQ6IAYOAAAACVRociBmd2Q6IAYPAAAAClRociBiYWNrOiAGEAAAAB4tLS0tLS1Jbml0IGNvbXBsZXRlZC0tLS0tLS0tLS0GEQAAAAhPdmVycmlkZQYSAAAABk1hc3M6IAYTAAAAAyBrZwYUAAAACFxuTVRPVzogBhUAAAAKXG5QYXlsb2FkOgYWAAAAAyAlIAYXAAAABENDOiAGGAAAAAtcbkFsdEhvbGQ6IAYZAAAAC1xuSG9ySG9sZDogBhoAAAAMXG5GbHl0b1BvbnQgBhsAAAAKXG5Ib2xkUG9zIAYcAAAADFxuSEdvckhvbGQ6IAYdAAAACVxuRGlyVG86IAYeAAAAC1ZlcnRTcGVlZDogBh8AAAAHXG5EaXN0IAYgAAAAASAL")))
                        IB_ = (string[])new BinaryFormatter().Deserialize(oK_); 
                }
                return IB_[hC_]; 
            } 
        }
        private string[] IB_;
    }
}