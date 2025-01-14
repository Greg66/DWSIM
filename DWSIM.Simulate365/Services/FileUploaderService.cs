﻿using DWSIM.Simulate365.Models;
using DWSIM.UI.Web.Settings;
using Microsoft.Graph;
using Microsoft.Graph.Ediscovery;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DWSIM.Simulate365.Services
{



    public class FileUploaderService
    {
        public static S365File UploadFile(string fileUniqueIdentifier, string parentUniqueIdentifier, string filePath, string filename, string simulatePath, UploadConflictAction? conflictAction)
        {
            using (var fileStream = System.IO.File.OpenRead(filePath))
                return UploadFile(fileUniqueIdentifier, parentUniqueIdentifier, fileStream, filename, simulatePath, conflictAction);
        }

        public static S365File UploadFileByFilePath(string simulatePath, Stream fileStream, UploadConflictAction? conflictAction)
        {
            try
            {
                if (simulatePath.StartsWith("//Simulate 365 Dashboard/"))
                    simulatePath = simulatePath.Substring(24);

                var fileWithBreadCrumbs = GetFileByPath(simulatePath);
                if (fileWithBreadCrumbs == null || fileWithBreadCrumbs.File == null)
                    throw new Exception($"File on simulate path '{simulatePath}' not found.");
                var file = fileWithBreadCrumbs.File;

                fileStream.Seek(0, SeekOrigin.Begin);

                var token = UserService.GetInstance().GetUserToken();
                var client = GetDashboardClient(token);
                var parentUniqueIdentifier = fileWithBreadCrumbs.BreadcrumbItems?.LastOrDefault()?.UniqueIdentifier.ToString();

                var filename= Path.GetFileName(simulatePath) ?? string.Empty;   

                var fileResp = Task.Run(async () => await UploadDocumentAsync(parentUniqueIdentifier, filename, fileStream, conflictAction)).Result;

                return new S365File(filename)
                {
                    FileUniqueIdentifier = fileResp.FileUniqueIdentifier.ToString(),
                    ParentUniqueIdentifier = parentUniqueIdentifier,
                    Filename = fileResp.Filename,
                    FullPath = fileResp.SimulatePath

                };

            }
            catch (Exception ex)
            {

                throw new Exception("An error occurred while saving file to Simulate 365 Dashboard.", ex);
            }
        }

        public static S365File UploadFile(string fileUniqueIdentifier, string parentUniqueIdentifier, Stream fileStream, string filename, string simulatePath, UploadConflictAction? conflictAction)
        {
            try
            {
                fileStream.Seek(0, SeekOrigin.Begin);

                var token = UserService.GetInstance().GetUserToken();
                var client = GetDashboardClient(token);

                var file = Task.Run(async () => await UploadDocumentAsync(parentUniqueIdentifier, filename, fileStream, conflictAction)).Result;

                return new S365File(filename)
                {
                    FileUniqueIdentifier = file.FileUniqueIdentifier.ToString(),
                    ParentUniqueIdentifier = parentUniqueIdentifier,
                    Filename = file.Filename,
                    FullPath = file.SimulatePath

                };

            }
            catch (Exception ex)
            {

                throw new Exception("An error occurred while saving file to Simulate 365 Dashboard.", ex);
            }
        }

        public static async Task<UploadFileResponseModel> UploadDocumentAsync(string parentUniqueIdentifier, string filename, Stream fileStream, UploadConflictAction? conflictAction)
        {


            try
            {
                var token = UserService.GetInstance().GetUserToken();
                var client = GetDashboardClient(token);

                using (var content = new MultipartFormDataContent())
                {
                    // 0= Overwrite file if exists, 1= Keep both
                    if (conflictAction.HasValue)
                    {
                        content.Add(new StringContent(conflictAction.ToString()), "ConflictAction");
                    }
                    if (!string.IsNullOrWhiteSpace(parentUniqueIdentifier))
                    {
                        content.Add(new StringContent(parentUniqueIdentifier), "ParentDirectoryUniqueId");
                    }

                    content.Add(new StreamContent(fileStream), "files", filename);

                    // Send request
                    var response = await client.PostAsync("/api/files/upload", content);

                    // Handle response
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseModel = JsonConvert.DeserializeObject<List<UploadFileResponseModel>>(responseContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        throw new Exception($"An error occurred while uploading file. Status code: {response.StatusCode}. Error:{errorMessage}");
                    }
                    if (responseModel == null || responseModel.Count == 0)
                    {
                        throw new Exception("An error occurred while uploading file. Response is empty.");
                    }

                    return responseModel.First();
                }

            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while trying to upload document.", ex);
            }
        }
        private static FilesWithBreadcrumbsResponseModel GetFileByPath(string simulatePath)
        {

            var token = UserService.GetInstance().GetUserToken();
            var client = GetDashboardClient(token);
            var result = Task.Run(async () => await client.GetAsync($"/api/files/by-path?filePath={simulatePath}&includeBreadcrumbs=true")).Result;
            var resultContent = Task.Run(async () => await result.Content.ReadAsStringAsync()).Result;
            var itemWithBreadcrumbs = JsonConvert.DeserializeObject<FilesWithBreadcrumbsResponseModel>(resultContent);
            return itemWithBreadcrumbs;
        }

        private static HttpClient GetDashboardClient(string token)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(DashboardSettings.DashboardServiceUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            return client;

        }

    }
}
