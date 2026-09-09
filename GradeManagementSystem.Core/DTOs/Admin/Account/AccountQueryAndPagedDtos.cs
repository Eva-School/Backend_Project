using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GradeManagementSystem.Core.DTOs.Admin.Account
{
    public class AccountListQueryDto
    {
        private int _pageNumber = 1;
        private int _pageSize = 10;
        private string? _search;

        [StringLength(100)]
        public string? Search
        {
            get => _search;
            set => _search = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public string? Role { get; set; }

        public bool? IsActive { get; set; }

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
        }

        public string? SortBy { get; set; } = "CreatedAt";

        public bool SortDescending { get; set; } = true;
    }

    public class AccountPagedResultDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
