﻿using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Telemedicine.Core.Data;
using Telemedicine.Core.Data.EntityFramework;
using Telemedicine.Core.Models;

namespace Telemedicine.Core.Domain.Repositories
{
    public class RecommendationRepository : EfRepositoryBase<Recommendation>, IRecommendationRepository
    {
        public RecommendationRepository(IDbContextProvider dbContextProvider) : base(dbContextProvider)
        {
        }


        public IEnumerable<Recommendation> GetAll()
        {
            return Set;
        }

        public async Task<IEnumerable<Recommendation>> GetPatientRecommendations(int patientId)
        {
            return await Set.Where(t => t.PatientId == patientId).ToListAsync();
        }
    }
}