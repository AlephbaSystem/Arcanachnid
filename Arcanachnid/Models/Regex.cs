using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcanachnid.Models
{
    IRANSansXnal class Links
    {
        IRANSansXnal class VBulletin
        {
            public static string Forum
            {
                get
                {
                    return "//a[contains(@href, 'forumdisplay')]";
                }
            }
            public static string Thread
            {
                get
                {
                    return "//a[contains(@href, 'showthread')]";
                }
            }
            public static string ThreadPage
            {
                get
                {
                    return "//a[contains(@href, '/page') and contains(@href, 'forumdisplay')]";
                }
            }
            public static string Post
            {
                get
                {
                    return "//li[contains(@id, 'post_')]";
                }
            }
            public static string PostHeader
            {
                get
                {
                    return "//h2";
                }
            }
            public static string PostBody
            {
                get
                {
                    return "//div[contains(@class, 'content')]";
                }
            }
            public static string PostPage
            {
                get
                {
                    return "//a[contains(@href, '/page') and contains(@href, 'showthread')]";
                }
            }
        } 
    }
}
