using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DataDiggersWebApp.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using Npgsql;
using OfficeOpenXml;
using ExcelDataReader;
using DataDiggersWebApp.AzureUpload;

namespace DataDiggersWebApp.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult Login()
        {
            return View();
        }
        public IActionResult FileUpload()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Login(LoginModel loginModel)
        {
           // login
            //await UploadFile(file);
            TempData["msg"] =loginModel.userName+ " has been successfully logged in";
            return Redirect("/Home/FileUpload");
        }
        // Upload file on server
        [HttpPost]
        public async Task<ActionResult> FileUpload(IFormFile file)
        {
            await UploadFile(file);
            TempData["msg"] = "File Uploaded successfully.";
            return View();
        }
        public async Task<bool> UploadFile(IFormFile file)
        {
            string path = "";
            bool iscopied = false;
            try
            {
                if (file.Length > 0)
                {
                    string filename =file.FileName;
                    path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Upload"));
                    using (var filestream = new FileStream(Path.Combine(path, filename), FileMode.Create))
                    {
                        await file.CopyToAsync(filestream);
                    }
                    iscopied = true;
                    UploadFile(path+"\\"+filename,filename);
                    //ReadExcelData(filename, path,file);

                    //upload to yugabyte
                    //NPGUpload();

                }
                else
                {
                    iscopied = false;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return iscopied;
        }

        public static void UploadFile(string srcPath,string filename)
        {
            AzureOperationHelper azureOperationHelper = new AzureOperationHelper();
            // your Storage Account Name  
            azureOperationHelper.storageAccountName = "bsdatadiggers";
            //azureOperationHelper.storageEndPoint = "https://bsdatadiggers.blob.core.windows.net";
            azureOperationHelper.storageEndPoint = "core.windows.net";
            // File path to upload  
            azureOperationHelper.srcPath = srcPath;
            // Your Container Name   
            azureOperationHelper.containerName = "inputfile";
            // Destination Path you can set it file name or if you want to put it in folders do it like below  
            azureOperationHelper.blobName = string.Format(Path.GetFileName(srcPath));
            azureOperationHelper.filename = filename;
            AzureOperations.UploadFile(azureOperationHelper);

            AzureOperations.DownloadFile(azureOperationHelper);

        }
        public async void ReadExcelData(string filename,string path, IFormFile file)
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using (var filestream = new FileStream(Path.Combine(path, filename), FileMode.Open, FileAccess.Read))
                {
                    {
                        using (var reader = ExcelReaderFactory.CreateReader(filestream))
                        {
                            do
                            {
                                while (reader.Read()) //Each ROW
                                {
                                    for (int column = 0; column < reader.FieldCount; column++)
                                    {
                                        //Console.WriteLine(reader.GetString(column));//Will blow up if the value is decimal etc. 
                                        Console.WriteLine(reader.GetValue(column));//Get Value returns object
                                    }
                                }
                            } while (reader.NextResult()); //Move to NEXT SHEET
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public void NPGUpload()
        {
            //NpgsqlConnection conn = new NpgsqlConnection("host=20.127.243.138;port=5433;database=yugabyte;user id=yugabyte;password=Hackathon22!");
            var connStringBuilder = new NpgsqlConnectionStringBuilder();
            connStringBuilder.Host = "20.127.243.138";
            connStringBuilder.Port = 5433;
            connStringBuilder.SslMode = SslMode.Require;
            connStringBuilder.Username = "yugabyte";
            connStringBuilder.Password = "Hackathon22!";
            connStringBuilder.Database = "yugabyte";
            connStringBuilder.TrustServerCertificate = true;
            NpgsqlConnection conn = new NpgsqlConnection(connStringBuilder.ConnectionString);
            try
            {
                
                conn.Open();

                //NpgsqlCommand empCreateCmd = new NpgsqlCommand("CREATE TABLE employee (id int PRIMARY KEY, name varchar, age int, language varchar);", conn);
                
                //CRUD(connStringBuilder.ConnectionString);
                NpgsqlCommand empDropCmd = new NpgsqlCommand("DROP TABLE if exists employee;", conn);
                empDropCmd.ExecuteNonQuery();
                Console.WriteLine("Dropped table Employee");

                NpgsqlCommand empCreateCmd = new NpgsqlCommand("CREATE TABLE employee (id int PRIMARY KEY, name varchar, age int, language varchar);", conn);

                empCreateCmd.ExecuteNonQuery();
                Console.WriteLine("Created table Employee");

                NpgsqlCommand empInsertCmd = new NpgsqlCommand("INSERT INTO employee (id, name, age, language) VALUES (1, 'John', 35, 'CSharp');", conn);
                int numRows = empInsertCmd.ExecuteNonQuery();
                Console.WriteLine("Inserted data (1, 'John', 35, 'CSharp')");

                NpgsqlCommand empPrepCmd = new NpgsqlCommand("SELECT name, age, language FROM employee WHERE id = @EmployeeId", conn);
                empPrepCmd.Parameters.Add("@EmployeeId", NpgsqlTypes.NpgsqlDbType.Integer);

                empPrepCmd.Parameters["@EmployeeId"].Value = 1;
                NpgsqlDataReader reader = empPrepCmd.ExecuteReader();

                Console.WriteLine("Query returned:\nName\tAge\tLanguage");
                while (reader.Read())
                {
                    Console.WriteLine("{0}\t{1}\t{2}", reader.GetString(0), reader.GetInt32(1), reader.GetString(2));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failure: " + ex.Message);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                {
                    conn.Close();
                }
            }
        }

    }
}
