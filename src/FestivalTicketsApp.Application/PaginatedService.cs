namespace FestivalTicketsApp.Application;

public abstract class PaginatedService
{
    protected IQueryable<T> ProcessPagination<T>(IQueryable<T> entitySource,
                                                 int pageNum,
                                                 int pageSize,
                                                 ref int nextPagesAmount)
    {
        int skipValues = (pageNum - 1) * pageSize;

        nextPagesAmount = (int)Math.Ceiling(
            (double)entitySource.Skip(skipValues + pageSize).Count() / pageSize);

        entitySource = entitySource.Skip(skipValues).Take(pageSize);

        return entitySource;
    }
}
