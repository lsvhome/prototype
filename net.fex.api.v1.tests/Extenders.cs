using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Net.Fex.Api.Tests
{
    public static class Extenders
    {
        public class RequestCaptchaAnswerResult : IDisposable
        {
            public CommandCaptcha.CommandCaptchaResult CommandCaptchaResult { get; set; }

            public string UserInput { get; set; }

            public void Dispose()
            {
                if (this.CommandCaptchaResult != null)
                {
                    this.CommandCaptchaResult.Dispose();
                    this.CommandCaptchaResult = null;
                }
            }
        }

        public static async Task<RequestCaptchaAnswerResult> RequestCaptchaAnswerAsync(this Net.Fex.Api.Connection conn, CommandCaptcha.CommandCaptchaResult captchaImage)
        {
            {
                var tmpFileName = System.IO.Path.GetTempFileName();
                var fileNameImage = tmpFileName + ".jpg";
                var fileNameTxt = tmpFileName + ".txt";
                Process processIexplore = null;
                Process processNotepad = null;
                try
                {
                    captchaImage.Image.Save(fileNameImage, System.Drawing.Imaging.ImageFormat.Jpeg);
                    await File.WriteAllTextAsync(fileNameTxt, string.Empty);
                    processIexplore = System.Diagnostics.Process.Start(@"C:\Program Files\Internet Explorer\iexplore.exe", fileNameImage);
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(3));
                    processNotepad = System.Diagnostics.Process.Start(@"C:\Windows\System32\notepad.exe", fileNameTxt);

                    if (processNotepad != null)
                    {
                        DateTime end = DateTime.Now.AddMinutes(5);
                        string s = null;
                        while (DateTime.Now < end && string.IsNullOrWhiteSpace(s))
                        {
                            try
                            {
                                if (new System.IO.FileInfo(fileNameTxt).Length > 0)
                                {
                                    s = File.ReadAllText(fileNameTxt);
                                    if (!string.IsNullOrWhiteSpace(s))
                                    {
                                        return new RequestCaptchaAnswerResult { CommandCaptchaResult = captchaImage, UserInput = s };
                                    }
                                }

                                System.Threading.Thread.Sleep(1000);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        if (new System.IO.FileInfo(fileNameTxt).Length == 0)
                        {
                            throw new CaptchaRequiredException();
                        }
                    }
                }
                catch (CaptchaRequiredException)
                {
                    throw;
                }
                finally
                {
                    if (processIexplore != null)
                    {
                        processIexplore.Kill();
                        processIexplore.Dispose();
                        processIexplore = null;
                    }

                    if (processNotepad != null)
                    {
                        processNotepad.Kill();
                        processNotepad.Dispose();
                        processNotepad = null;
                    }

                    System.IO.File.Delete(fileNameImage);
                    System.IO.File.Delete(fileNameTxt);
                    System.IO.File.Delete(tmpFileName);
                }
            }

            throw new CaptchaRequiredException();
        }
    }
}
