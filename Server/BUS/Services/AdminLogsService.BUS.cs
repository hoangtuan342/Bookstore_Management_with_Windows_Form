using System;
using System.Collections.Generic;
using TrangSach.Shared.DTO;

namespace TrangSach.BUS
{
    public class AdminLogsService
    {
        private readonly BookStoreDao dao;

        public AdminLogsService(BookStoreDao dao)
        {
            this.dao = dao;
        }

        public IReadOnlyList<ManagerAuditLogDto> GetLogs()
        {
            return dao.Users.GetAuditLogs(500);
        }
    }
}
