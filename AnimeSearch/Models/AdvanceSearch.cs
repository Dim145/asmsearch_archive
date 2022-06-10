using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace AnimeSearch.Models
{
    public class AdvanceSearch
    {
        public int Page { get; set; } = 1;
        public string[] With_genres { get; set; }
        public string[] Without_genres { get; set; }
        public string Q { get; set; }
        [BindProperty]
        public DateTime? After { get; set; }
        [BindProperty]
        public DateTime? Before { get; set; }
        public int SortBy { get; set; } = -1;
        public bool AndGenre { get; set; } = true;
        public SearchType SearchIn { get; set; } = SearchType.ALL;

        public bool IsEmpty()
        {
            return 
                (With_genres    == null || With_genres.Length    == 0) && 
                (Without_genres == null || Without_genres.Length == 0) && 
                After  == null && 
                Before == null && 
                string.IsNullOrWhiteSpace(Q);
        }

        public enum SearchType: byte
        {
            ALL,
            SERIES,
            MOVIES
        }
    }
}
