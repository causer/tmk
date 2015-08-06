﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Telemedicine.Core.Data;
using Telemedicine.Core.Data.EntityFramework;
using Telemedicine.Core.Models;
using Telemedicine.Core.PagedList;

namespace Telemedicine.Core.Domain.Repositories
{
    public class AppointmentEventRepository : EfRepositoryBase<AppointmentEvent>, IAppointmentEventRepository
    {
        public AppointmentEventRepository(IDbContextProvider dbContextProvider) : base(dbContextProvider)
        {
        }

        public async override Task<IEnumerable<AppointmentEvent>> GetAllAsync()
        {
            return await Set.Include(t => t.Patient).Include(p=>p.Patient.User)
                .Include(t => t.Doctor).Include(t=>t.Doctor).Include(t => t.Doctor.User)
                .ToListAsync();
        }

        public async Task<IPagedList<AppointmentEvent>> GetDoctorAppointmentsPagedAsync(int doctorId, int page, int pageSize, string patientTitleFilter, DateTime? start, DateTime? end)
        {
            var query = Set
                .Include(t => t.Patient)
                .Include(t => t.Patient.User)
                .Where(t => t.DoctorId == doctorId);

            if (start.HasValue && end.HasValue)
            {
                query = query.Where(t => t.Date >= start && t.Date < end);
            }
            else
            {
                query = query.Where(t => t.Date >= DateTime.Now);
            }

            if (!string.IsNullOrWhiteSpace(patientTitleFilter))
            {
                var searchString = patientTitleFilter.Trim();
                var groups = searchString.Split(' ');
                if (groups.Length > 0)
                {
                    var ss = groups[0];
                    query = query.Where(t => t.Patient.User.LastName.ToUpper().Contains(ss.ToUpper()));
                }
                if (groups.Length > 1)
                {
                    var ss = groups[1];
                    query = query.Where(t => t.Patient.User.FirstName.ToUpper().Contains(ss.ToUpper()));
                }
                if (groups.Length > 2)
                {
                    var ss = groups[2];
                    query = query.Where(t => t.Patient.User.MiddleName.ToUpper().Contains(ss.ToUpper()));
                }
            }

            return await query.OrderBy(t => t.Date).ToPagedListAsync(page, pageSize);
        }

        public async Task<IEnumerable<AppointmentEvent>> GetDoctorAppointmentsByDateAsync(int doctorId, DateTime date)
        {
            var begin = date.Date;
            var end = date.AddDays(1).Date;
            var appointments = await Set.Include(t => t.Patient).Include(p => p.Patient.User)
                .Where(a => a.Date >= begin && a.Date < end)
                .ToListAsync();
            return appointments;
        }

        public async Task<IPagedList<AppointmentEvent>> GetPatientAppointmentsPagedAsync(int patientId, int page, int pageSize)
        {
            var query = Set
                .Include(t => t.Doctor)
                .Include(t => t.Doctor.User)
                .Where(t => t.PatientId == patientId)
                .Where(t => t.Date >= DateTime.Now);

            return await query.OrderBy(t => t.Date).ToPagedListAsync(page, pageSize);
        }
    }
}