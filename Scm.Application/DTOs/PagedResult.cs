using System;
using System.Collections.Generic;

namespace Scm.Application.DTOs;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public int PageNumber { get; init; }
        = 1;

    public int PageSize { get; init; }
        = 1;

    public int TotalCount { get; init; }
        = 0;

    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => PageNumber > 1;

    public bool HasNext => PageNumber < TotalPages;

    public int StartRecord => TotalCount == 0
        ? 0
        : (PageNumber - 1) * PageSize + 1;

    public int EndRecord => TotalCount == 0
        ? 0
        : Math.Min(PageNumber * PageSize, TotalCount);
}
