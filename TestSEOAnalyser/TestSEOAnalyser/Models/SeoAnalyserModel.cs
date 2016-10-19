using BLL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TestSEOAnalyser.Models
{
    public class SeoAnalyserModel
    {
        [Required]
        [DisplayName ("Input text or URL")]
        public string InputText { get; set; }
        [DisplayName ("Calculate stop-words")]
        public bool CalculateStopWords { get; set; }
        [DisplayName ("Calculate words")]
        public bool CalculateWords { get; set; }
        [DisplayName ("Calculate words in meta tags")]
        public bool CalculateWordsInTags { get; set; }
        [DisplayName ("Calculate external links")]
        public bool CalculateExternalLinks { get; set; }
    }

    public class SeoAnalyserResultModel
    {
        public string InputText { get; set; }
        public Dictionary<string, int> StopWords { get; set; }
        public Dictionary<string, int> Words { get; set; }
        public Dictionary<string, int> WordsInTag { get; set; }
        public List<string> ExternalLinks { get; set; }
        public string Error { get; set; }

        /// <summary>
        /// constructor initializes all properties according to unput model through AnalyserBLana
        /// </summary>
        /// <param name="model"></param>
        public SeoAnalyserResultModel(SeoAnalyserModel model)
        {
            //create AnalyzerBL instance 
            AnalyzerBL analyser = new AnalyzerBL(model.InputText);

            if (!string.IsNullOrEmpty(analyser.Error))
            {
                Error = analyser.Error;
                return;
            }

            if(model.CalculateStopWords)
                StopWords = analyser.GetStopWordsOccurrences();

            if(model.CalculateWords)
                Words = analyser.GetWordsOccurrences();

            if(model.CalculateExternalLinks)
                ExternalLinks = analyser.GetExternalLinksOccurrences();

            if (model.CalculateWordsInTags)
                WordsInTag = analyser.GetWordsInTagsOccurrences();
    
        }
    }
}