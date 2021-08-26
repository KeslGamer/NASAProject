using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IO;

namespace NASAProject.Controllers
{

	public class HomeController : Controller
	{
        private readonly IBrainstormSessionRepository _sessionRepository;

        public HomeController(IBrainstormSessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
        }

        public async Task<IActionResult> Index()
		{
			var photoList = new List<Root>();
			var lines = System.IO.File.ReadAllLines("dates.txt").ToList();

			foreach (var date in lines)
			{
				var temp = await PhotoList(date);
				if (temp != null)
				{
					photoList.Add(temp);
				}
			}

			return View(photoList);
		}

		private static async Task<Root> PhotoList(string date)
		{
			Root photoList;

			DateTime dateConvert;
			var dateConverted = DateTime.TryParse(date, out dateConvert);
			if (!dateConverted)
			{
				return null;
			}

			using (var httpClient = new System.Net.Http.HttpClient())
			{
				using (var response = await httpClient.GetAsync(
					"https://api.nasa.gov/mars-photos/api/v1/rovers/curiosity/photos?earth_date=" + dateConvert.ToString("yyyy-M-d") + "&api_key=DEMO_KEY"))
				{
					var apiResponse = await response.Content.ReadAsStringAsync();

					photoList = JsonConvert.DeserializeObject<Root>(apiResponse);
				}

                if (photoList == null) return null;

                foreach (var photo in photoList.photos)
                {
                    // Create file path and ensure directory exists
                    var directoryPath = "App_Data/" + photo.earth_date;
                    var path = Path.Combine(directoryPath, $"{photo.id + photo.earth_date}.jpg");
                    Directory.CreateDirectory(directoryPath);

                    // Download the image and write to the file
                    var imageBytes = await httpClient.GetByteArrayAsync(photo.img_src);
                    System.IO.File.WriteAllBytes(path, imageBytes);
                }
            }

			return photoList;
		}

		public IActionResult Error()
		{
			return View();
		}
	}

    public interface IBrainstormSessionRepository
    {
    }

    public class Camera
	{
		public int id { get; set; }
		public string name { get; set; }
		public int rover_id { get; set; }
		public string full_name { get; set; }
	}

	public class Rover
	{
		public int id { get; set; }
		public string name { get; set; }
		public string landing_date { get; set; }
		public string launch_date { get; set; }
		public string status { get; set; }
	}

	public class Photo
	{
		public int id { get; set; }
		public int sol { get; set; }
		public Camera camera { get; set; }
		public string img_src { get; set; }
		public string earth_date { get; set; }
		public Rover rover { get; set; }
		public string error { get; set; }
	}

	public class Root
	{
		public List<Photo> photos { get; set; }
	}
}
