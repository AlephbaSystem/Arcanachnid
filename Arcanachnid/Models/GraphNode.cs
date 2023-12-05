using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcanachnid.Models
{
    public class GraphNode
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public string Url { get; set; }
        public string PostId { get; set; }
        public List<GraphNode> ChildNodes { get; set; }

        public GraphNode(string title, string body, string url, string postId)
        {
            Title = title;
            Body = body;
            Url = url;
            PostId = postId;
            ChildNodes = new List<GraphNode>();
        }
        public GraphNode()
        {
            ChildNodes = new List<GraphNode>();
        }
    }

}
