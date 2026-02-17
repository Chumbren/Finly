using System;
using System.Collections.Generic;
using System.Text;

namespace Finly.Models
{
    // Сообщение для обновления данных на главном экране
    public class DataChangedMessage
    {
        // Можно добавить свойства, если нужно передавать дополнительную информацию
        public string? EntityType { get; set; } // "Transaction", "Account", "Category"
        public int? EntityId { get; set; }
        public ChangeType ChangeType { get; set; } = ChangeType.Added;
    }

    public enum ChangeType
    {
        Added,
        Updated,
        Deleted
    }
}
