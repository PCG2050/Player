using System;
using System.Collections.Generic;
using System.Text;

namespace Player.Models
{
    public class Question
    {
        public string QuestionText { get; set; }
        public List<String> Options { get; set; }
        public string CorrectAnswer { get; set; }
        public TimeSpan TimeStamp { get; set; }


    }
}
