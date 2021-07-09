using MvcBlazorDemo.Data;
using FD.SampleData.Data;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Data;
using MvcBlazorDemo.Models;

namespace MvcBlazorDemo
{
    public interface IDataService
    {
        Task<DataTable> Command(string query);
        Task<List<TraceLog>> GetTraceLogs();
    }

    public class DataService : IDataService
    {
        private readonly ILogger<DataService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly DbConnectionRestricted _connectionRestricted;

        public DataService(ILogger<DataService> logger, ApplicationDbContext context, DbConnectionRestricted connectionRestricted)
        {
            _logger = logger;
            _context = context;
            _connectionRestricted = connectionRestricted;
        }

        public async Task<DataTable> Command(string query)
        {
            var cmd = _connectionRestricted.CreateCommand();
            cmd.CommandText = query;
            var result = await cmd.ExecuteReaderAsync();
            var dt = new DataTable();
            dt.Load(result);

            return dt;
        }

        public async Task<List<TraceLog>> GetTraceLogs()
        {
            return await _context.TraceLogs
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
