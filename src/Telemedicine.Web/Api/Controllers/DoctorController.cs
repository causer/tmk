﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using AutoMapper;
using Telemedicine.Core.Consts;
using Telemedicine.Core.Domain.Services;
using Telemedicine.Web.Api.Dto;
using Telemedicine.Web.Areas.Doctor.Models.Timetable;

namespace Telemedicine.Web.Api.Controllers
{
    [RoutePrefix("api/v1/doctor")]
    [Authorize]
    public class DoctorController : ApiController
    {
        private readonly IDoctorService _doctorService;
        private readonly IAppointmentEventService _appointmentService;
        private readonly ITimeSpanEventService _timeSpanService;
        private readonly IDoctorPaymentService _doctorPaymentService;
        private readonly IDoctorTimetableService _doctorTimetableService;
        public DoctorController(
            IDoctorService doctorService, 
            IAppointmentEventService appointmentService, 
            ITimeSpanEventService timeSpanService,
            IDoctorPaymentService doctorPaymentService, IDoctorTimetableService doctorTimetableService)
        {
            _doctorService = doctorService;
            _appointmentService = appointmentService;
            _timeSpanService = timeSpanService;
            _doctorPaymentService = doctorPaymentService;
            _doctorTimetableService = doctorTimetableService;
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetDoctors(int page = 1, int pageSize = 50, string titleFilter = null, int specializationIdFilter = 0)
        {
            try
            {
                var doctors = await _doctorService.PagedAsync(page, pageSize, titleFilter, specializationIdFilter);
                var pagedList = doctors.Map(t =>
                {
                    var doctorDto = Mapper.Map<DoctorListItemDto>(t);
                    doctorDto.StatusText = doctorDto.StatusName == DoctorStatusNames.Busy ? "занят" : "доступен";
                    doctorDto.IsChatAvailable = t.DoctorStatus.Name != DoctorStatusNames.Busy;
                    doctorDto.IsAudioAvailable = t.DoctorStatus.Name == DoctorStatusNames.VideoChat;
                    doctorDto.IsVideoAvailable = t.DoctorStatus.Name == DoctorStatusNames.VideoChat;
                    return doctorDto;
                });
                return Ok(pagedList);
            }
            catch (Exception)
            {
                return NotFound();
            }
            
        }

        [HttpGet]
        [Route("{id}/details")]
        public async Task<IHttpActionResult> Details(int id)
        {
            var doctor = await _doctorService.GetByIdAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }

            return Ok(doctor.Description);
        }

        [HttpGet]
        [Route("{id}/appointments")]
        public async Task<IHttpActionResult> Appointments(int id, int page = 1, int pageSize = 10, string patientTitleFilter = null)
        {
            var doctorAppointments = await _appointmentService.GetDoctorAppointmentsPagedAsync(id, page, pageSize, patientTitleFilter);
            var pagedList = doctorAppointments.Map(t =>
            {
                var doctorAppointmentDto = Mapper.Map<DoctorAppointmentDto>(t);
                doctorAppointmentDto.PatientAvatarUrl = "/img/no_avatar.png";
                return doctorAppointmentDto;
            });

            return Ok(pagedList);
        }

        [HttpGet]
        [Route("{id}/appointments/{date}")]
        public async Task<IHttpActionResult> Appointments(int id, DateTime date)
        {
            var doctorAppointments = await _appointmentService.GetDoctorAppointmentsByDateAsync(id, date);
            return Ok(doctorAppointments);
        }

        [HttpGet]
        [Route("{id}/timeWindows")]
        public async Task<IHttpActionResult> TimeWindows(int id)
        {
            var doctorTimeWindows = await _timeSpanService.GetDoctorTimeWindowsAsync(id);
            return Ok(doctorTimeWindows);
        }

        [HttpGet]
        [Route("{doctorId}/paymentHistory")]
        public async Task<IHttpActionResult> PaymentPage(int doctorId, int page = 1, int pageSize = 10)
        {
            var doctor = await _doctorService.GetByIdAsync(doctorId);

            if (doctor == null)
            {
                return NotFound();
            }
            var payments = await _doctorPaymentService.PagedAsync(doctorId, page, pageSize);
            // var payments = await _paymentHistoryService.PagedAsync(User.Identity.GetUserId(), page, pageSize);
            var pagedList = payments.Map(t =>
            {
                var doctorDto = Mapper.Map<PaymentHistoryDto>(t);
                return doctorDto;
            });

            return Ok(pagedList);
        }

        [HttpGet]
        [Route("{doctorId}/timetable/{year}/{month}")]
        public async Task<IHttpActionResult> GetMonthTimetable(int doctorId, int year, int month)
        {
            var timetableViewModel = new List<TimetableDateViewModel>();
            var timetable = await _doctorTimetableService.GetDoctorTimetableByMonthAsync(doctorId, year, month);

            foreach (var time in timetable)
            {
                var timeViewModel = timetableViewModel.FirstOrDefault(t => t.Date == time.DateTime.Date);
                if (timeViewModel == null)
                {
                    timeViewModel = new TimetableDateViewModel { Date = time.DateTime.Date };
                    timetableViewModel.Add(timeViewModel);
                }

                //проверка есть ли хотя бы один пациент на дату
                timeViewModel.HasConsultations = timeViewModel.HasConsultations || (time.AppointmentEvents != null && time.AppointmentEvents.Any());
                timeViewModel.Hours = timeViewModel.Hours ?? new List<TimetableHourViewModel>();

                var hour = time.DateTime.Hour;
                var hourViewModel = timeViewModel.Hours.FirstOrDefault(t => t.Hour == hour);
                if (hourViewModel == null)
                {
                    hourViewModel = new TimetableHourViewModel {Hour = hour, HourType = time.HourType};
                    timeViewModel.Hours.Add(hourViewModel);
                }

                //подсчет пациентов
                if (time.AppointmentEvents != null)
                    hourViewModel.PatientsCount = time.AppointmentEvents.Count;
            }

            return Ok(timetableViewModel);
        }
    }
}
