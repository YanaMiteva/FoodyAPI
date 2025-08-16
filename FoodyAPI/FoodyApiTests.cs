using FoodyAPI.Models;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Xml.Linq;

namespace FoodyAPI
{
    public class FoodTests
    {
        private RestClient client;
        private static string? createdFoodId;
        private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";
        
        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("TestFoodyUser", "TestFoodyUser123!");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new {username, password});
            
            var response = loginClient.Execute(request);
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [Test, Order(1)]
        public void CreateFood_WithRequiredFields_ShouldReturnCreatedAndFoodId()
        {
            var newFood = new FoodDTO
            {
                Name = "New Food 1",
                Description = "New description 1"
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(newFood);
            var response = client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(createResponse, Has.Property("FoodId"));
            Assert.That(createResponse?.FoodId, Is.Not.Null.And.Not.Empty);

            createdFoodId = createResponse?.FoodId;
        }

        [Test, Order(2)]
        public void EditFoodTitle_ShouldReturnOkAndSuccessMessage()
        {
            var newTitle = "Edited Title";

            var patchBody = new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = newTitle

                }
            };

            var request = new RestRequest($"/api/Food/Edit/{createdFoodId}", Method.Patch);
            request.AddJsonBody(patchBody);
            var response = client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO> (response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllFoods_ShouldReturnOkAndNonEmptyArray()
        {
            var request = new RestRequest("/api/Food/All");
            var response = client.Execute(request);
            var foods = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);
            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(foods, Is.Not.Null);
            Assert.That(foods.Count, Is.GreaterThan(0));


        }

        [Test, Order(4)]
        public void DeleteEditedFood_ShouldReturnOkAndSuccessMessage()
        {
            var request = new RestRequest($"/api/Food/Delete/{createdFoodId}", Method.Delete);
            var response = client.Execute(request);
            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(deleteResponse.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateFood_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var foodRequest = new FoodDTO
            {
                Name = "",
                Description = ""
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(foodRequest);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingFood_ShouldReturnNotFoundAndNoFoodMessage()
        {
            var nonExistingFoodId = "12345";

            var editFood = new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "new title"
                }
            };

            var request = new RestRequest($"/api/Food/Edit/{nonExistingFoodId}", Method.Patch);
            request.AddJsonBody(editFood);
            var response = client.Execute(request);
            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No food revues..."));
        }

        [Test, Order(7)]
        public void DeleteNonExistingFood_ShouldReturnBadRequestAndUnableToDeleteMessage()
        {
            var nonExistingFoodId = "12345";
            var request = new RestRequest($"/api/Food/Delete/{nonExistingFoodId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this food revue!"));

        }

        [OneTimeTearDown]
        public void TearDown()
        {
            client.Dispose();
        }
    }
}