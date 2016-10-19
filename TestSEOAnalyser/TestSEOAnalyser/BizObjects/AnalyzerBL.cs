using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;
using System.Web;

namespace BLL
{
    /// <summary>
    /// class analyses input data and compute all necessary information
    /// </summary>
    public class AnalyzerBL
    {
        #region constructors
        public AnalyzerBL(string inputText)
        {
            // check if input text is empty
            if (string.IsNullOrEmpty(inputText))
            {
                Error = STRING_EMPTY_ERROR;
                return;
            }

            //check if input text is valid url
            if (IsValidURL(inputText))
            {
                GetTextFromURL(inputText);
            }
            else
            {
                _text = inputText;
            }

            //if _text is not empty initialize fields
            if (!string.IsNullOrEmpty(_text))
            {
                _document = new HtmlDocument();
                _document.LoadHtml(_text);
                _stopWords = System.IO.File.ReadAllLines(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, STOP_WORDS_FILE_PATH));
            }
        }
        #endregion

        #region public logic

        /// <summary>
        /// get occurrences of all words
        /// </summary>
        /// <returns>dictionary with all words where Key is word and Value is number of occurrences</returns>
        public Dictionary<string, int> GetWordsOccurrences()
        {
            var parsedString = ConvertHtmlToText();

            if (string.IsNullOrEmpty(parsedString))
                return null;

            return parsedString.Split(SPLIT_PATTERN, StringSplitOptions.RemoveEmptyEntries)
                        .Where(it => _stopWords.All(itm => itm.ToLower() != it.ToLower()))
                        .GroupBy(it => it)
                        .OrderByDescending(it => it.Count())
                        .ToDictionary(p => p.Key, p => p.Count());
        }

        /// <summary>
        /// get occurrences of stop words
        /// </summary>
        /// <returns>dictionary with stop words where Key is word and Value is number of occurrences</returns>
        public Dictionary<string, int> GetStopWordsOccurrences()
        {
            var parsedString = ConvertHtmlToText();

            if (string.IsNullOrEmpty(parsedString))
                return null;

            return parsedString.Split(SPLIT_PATTERN, StringSplitOptions.RemoveEmptyEntries)
                        .Where(it => _stopWords.Any(itm => itm.ToLower() == it.ToLower()))
                        .GroupBy(it => it)
                        .OrderByDescending(it => it.Count())
                        .ToDictionary(p => p.Key, p => p.Count());
        }

        /// <summary>
        /// get occurrences of words have been found in meta tags
        /// </summary>
        /// <returns>dictionary with words where Key is word and Value is number of occurrences</returns>
        public Dictionary<string, int> GetWordsInTagsOccurrences()
        {
            var parsedString = GetMetaTagsFromHtml();

            if (string.IsNullOrEmpty(parsedString))
                return null;

            return parsedString.Split(SPLIT_PATTERN, StringSplitOptions.RemoveEmptyEntries)
                        .GroupBy(it => it)
                        .OrderByDescending(it => it.Count())
                        .ToDictionary(p => p.Key, p => p.Count());
        }

        /// <summary>
        /// get external links from text
        /// </summary>
        /// <returns>returns list of external links</returns>
        public List<string> GetExternalLinksOccurrences()
        {
            if (_document == null)
                return null;

            //select all <a> tags with href attribute
            var links = _document.DocumentNode.SelectNodes(HREF_NODE);
            if (links == null)
                return null;

            var result = new List<string>();
            foreach (var link in links)
            {
                var href = link.Attributes[HREF_ATTRIBUTE].Value;
                //check whether text from href attribute is valid url or not
                if (IsValidURL(href))
                    result.Add(href);
            }

            return result;
        }

        #endregion

        #region private logic

        /// <summary>
        /// check whether input string is valid url or not
        /// </summary>
        /// <param name="inputUrl">input string</param>
        /// <returns></returns>
        private bool IsValidURL(string inputUrl)
        {
            if (string.IsNullOrEmpty(inputUrl))
                return false;

            Uri outputUri;

            return Uri.TryCreate(inputUrl, UriKind.Absolute, out outputUri)
                && (outputUri.Scheme == Uri.UriSchemeHttp || outputUri.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Get encoded html text from input url
        /// </summary>
        /// <param name="inputUrl">input url</param>
        private void GetTextFromURL(string inputUrl)
        {

            using (WebClient client = new WebClient())
            {
                try
                {
                    var bytes = Encoding.Default.GetBytes(client.DownloadString(inputUrl));
                    _text = Encoding.UTF8.GetString(bytes);
                }
                catch (WebException ex)
                {
                    Error = ex.Message;
                }
            }
        }

        /// <summary>
        /// convert html to text
        /// </summary>
        /// <returns>returns parsed string without html tags</returns>
        private string ConvertHtmlToText()
        {
            if (_document == null)
                return null;

            //select all nodes except time, style and scripts 
            IEnumerable<HtmlNode> nodes = _document.DocumentNode.Descendants()
                .Where(it =>
                    it.NodeType == HtmlNodeType.Text &&
                    it.ParentNode.Name != SCRIPT_NODE &&
                    it.ParentNode.Name != TIME_NODE &&
                    it.ParentNode.Name != STYLE_NODE);

            if (nodes == null)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (var node in nodes)
            {
                if (string.IsNullOrEmpty(node.InnerText))
                    continue;

                //split text from inner text
                var res = Regex.Replace(WebUtility.HtmlDecode(node.InnerText), TEXT_REGEX_PATTERN, COMA)
                    .Split(SPLIT_PATTERN)
                    .Select(it => it.Trim(TRIM_PATTERN).ToUpper());
                sb.Append(string.Join(COMA, res));
            }

            return sb.ToString();
        }

        /// <summary>
        /// get text from meta tags
        /// </summary>
        /// <returns>returns parsed string with text from meta tags</returns>
        private string GetMetaTagsFromHtml()
        {
            if (_document == null)
                return null;

            //select meta tags nodes
            var nodes = _document.DocumentNode.SelectNodes(META_NODE);

            if (nodes == null)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var item in nodes)
            {
                //node must contain name and content attributes
                if (item.Attributes[NAME_ATTRIBUTE] == null || item.Attributes[CONTENT_ATTRIBUTE] == null)
                    continue;

                //split text from content
                var res = Regex.Replace(item.Attributes[CONTENT_ATTRIBUTE].Value, TEXT_REGEX_PATTERN, COMA)
                    .Split(SPLIT_PATTERN)
                    .Select(it => it.Trim(TRIM_PATTERN).ToUpper() + WHITESPACE);
                sb.Append(string.Join(COMA, res));
            }

            return sb.ToString();
        }

        #endregion

        #region fields

        //properties
        public string Error { get; set; }

        //constants
        private const string SCRIPT_NODE = "script";
        private const string STYLE_NODE = "style";
        private const string TIME_NODE = "time";
        private const string HREF_NODE = "//a[@href]";
        private const string META_NODE = "//meta";
        private const string HREF_ATTRIBUTE = "href";
        private const string NAME_ATTRIBUTE = "name";
        private const string CONTENT_ATTRIBUTE = "content";
        private const string COMA = ",";
        private const string WHITESPACE = " ";
        private const string STRING_EMPTY_ERROR = "Input text is empty";
        private const string STOP_WORDS_FILE_PATH = @"Content\StopWords.txt";
        private const string TEXT_REGEX_PATTERN = @"[^’'`\&.a-zA-Z\-]+";
        private readonly char[] SPLIT_PATTERN = new[] { ' ', ',' };
        private readonly char[] TRIM_PATTERN = new[] { '.', '-', '&', '’', '\'' };
        private readonly string[] _stopWords;

        //private fields
        private string _text;
        private readonly HtmlDocument _document;

        #endregion
    }

}
