using Center.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Center.Services
{
    public class CenterApiService
    {
        private readonly HttpClient _http;

        public CenterApiService(string baseUrl)
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<List<StationInfo>> GetStationsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<StationInfo>>("/api/stations")
                       ?? new List<StationInfo>();
            }
            catch { return new List<StationInfo>(); }
        }

        public async Task<List<AlertEntry>> GetAlertsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<AlertEntry>>("/api/alerts")
                       ?? new List<AlertEntry>();
            }
            catch { return new List<AlertEntry>(); }
        }

        public async Task<bool> AcknowledgeAlertAsync(Guid alertId)
        {
            try
            {
                var resp = await _http.PostAsync($"/api/alerts/{alertId}/acknowledge", null);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
