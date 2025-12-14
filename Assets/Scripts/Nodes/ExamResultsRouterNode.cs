using System.Collections.Generic;
using UnityEngine;

namespace VNEngine
{
    public class ExamResultsRouterNode : Node
    {
        [System.Serializable]
        public class ScoreRoute
        {
            public int minScore = 0;                 // inclusive
            public ConversationManager conversation; // started if score >= minScore
        }

        [Header("Stat Keys")]
        public string studyGameScoreKey = "StudyGameScore";

        [Header("Score Tier Routes (highest minScore that matches wins)")]
        public List<ScoreRoute> routes = new List<ScoreRoute>();

        [Header("Fallback")]
        public ConversationManager fallbackConversation;

        [Header("Optional: Update Grades (midterm + final)")]
        public bool updateGrades = true;
        public string currentExamIdKey = "CurrentExamId";
        public string midtermsExamId = "EXAM_MIDTERMS";
        public string finalsExamId   = "EXAM_FINALS";
        public string gradesKey = "Grades";
        public string midtermScoreKey = "MidtermScore";
        public string finalScoreKey   = "FinalScore";
        public bool gradesUseAverage  = true;
        public float clampGradesMax   = 4f;

        public override void Run_Node()
        {
            int score = (int)StatsManager.Get_Numbered_Stat(studyGameScoreKey);
            Debug.Log($"[ExamResultsRouterNode] score={score}");

            if (updateGrades)
                ApplyScoreToGrades(score);

            var chosen = PickRoute(routes, score) ?? fallbackConversation;

            if (chosen != null)
            {
                chosen.Start_Conversation();
                go_to_next_node = false;
                Finish_Node();
                return;
            }

            go_to_next_node = true;
            Finish_Node();
        }

        private ConversationManager PickRoute(List<ScoreRoute> list, int score)
        {
            if (list == null || list.Count == 0) return null;

            ConversationManager best = null;
            int bestMin = int.MinValue;

            foreach (var r in list)
            {
                if (r == null || r.conversation == null) continue;
                if (score >= r.minScore && r.minScore >= bestMin)
                {
                    bestMin = r.minScore;
                    best = r.conversation;
                }
            }
            return best;
        }

        private void ApplyScoreToGrades(int score)
        {
            string examId = StatsManager.Get_String_Stat(currentExamIdKey);

            if (examId == midtermsExamId)
                StatsManager.Set_Numbered_Stat(midtermScoreKey, score);
            else if (examId == finalsExamId)
                StatsManager.Set_Numbered_Stat(finalScoreKey, score);
            else
                return; // not an exam, don't touch Grades

            float mid = StatsManager.Get_Numbered_Stat(midtermScoreKey);
            float fin = StatsManager.Get_Numbered_Stat(finalScoreKey);

            float combined = gradesUseAverage ? ((mid + fin) * 0.5f) : (mid + fin);
            if (clampGradesMax > 0f) combined = Mathf.Clamp(combined, 0f, clampGradesMax);

            StatsManager.Set_Numbered_Stat(gradesKey, combined);
            Debug.Log($"[ExamResultsRouterNode] {gradesKey}={combined} (mid={mid}, fin={fin})");
        }

        public override void Button_Pressed() { }
    }
}
