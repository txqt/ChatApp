﻿using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    public class Message
    {
        public int MessageId { get; set; }
        public int ChatId { get; set; }
        public string SenderId { get; set; }
        public string? Content { get; set; } = string.Empty;
        public MessageType MessageType { get; set; } = MessageType.Text;
        /// <summary>
        /// JSON array of media IDs, e.g. "[1,5,9]"
        /// </summary>
        public string? MediaFileIdsJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public bool IsEdited { get; set; } = false;

        // Reply/Thread support
        public int? ReplyToMessageId { get; set; }
        public int? ThreadRootMessageId { get; set; }

        // Forward support
        public int? ForwardedFromChatId { get; set; }
        public int? ForwardedFromMessageId { get; set; }

        // Navigation properties
        public Chat Chat { get; set; } = null!;
        public ApplicationUser Sender { get; set; } = null!;
        public Message? ReplyToMessage { get; set; }
        public Message? ThreadRootMessage { get; set; }
        public ApplicationUser? DeletedByUser { get; set; }
        public ICollection<MessageStatus> MessageStatuses { get; set; } = new List<MessageStatus>();
        public ICollection<Message> Replies { get; set; } = new List<Message>();
        public ICollection<Message> ThreadMessages { get; set; } = new List<Message>();

        [NotMapped]
        public List<int> MediaFileIds
        {
            get => string.IsNullOrEmpty(MediaFileIdsJson)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(MediaFileIdsJson)!;
            set => MediaFileIdsJson = JsonSerializer.Serialize(value);
        }
    }
}
