﻿using System;

namespace HA4IoT.Contracts.Services.Notifications
{
    public interface INotificationService : IService
    {
        void Create(NotificationType type, string text, TimeSpan timeToLive);

        void CreateInformation(string text);

        void CreateWarning(string text);

        void CreateError(string text);
    }
}
