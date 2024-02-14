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
        public string Category { get; set; }
        public string Body { get; set; }
        public string Url { get; set; }
        public string PostId { get; set; }
        public string Date { get; set; }
        public List<(string, string)> References { get; set; }
        public List<string> Tags { get; set; }
        public List<GraphNode> ChildNodes { get; set; }

        public GraphNode(string title, string body, string category, string url, string postId, string date, List<(string, string)> references, List<string> tags)
        {
            Title = title;
            Body = body;
            Url = url;
            Tags = tags;
            PostId = postId;
            Date = date;
            references = references;
            category = category;
            ChildNodes = new List<GraphNode>();
        }
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