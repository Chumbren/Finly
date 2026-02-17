using SQLite;
using System;

namespace Finly.Models
{
    public class Category
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public CategoryType Type { get; set; }

        public string Icon { get; set; } = "❓";

        public string Color { get; set; } = "#6200EA";

        public bool IsFavorite { get; set; }

        public int? ParentCategoryId { get; set; }
    }

    public enum CategoryType
    {
        Income,
        Expense
    }
}