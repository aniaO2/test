﻿namespace Organizer.Server.Models.Assistant
{
    public class EvaluateRequest
    {
        public string UserId { get; set; }
        public List<TaskDto> TodayTasks { get; set; }
    }
}
