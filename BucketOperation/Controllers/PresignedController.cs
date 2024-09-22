using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace BucketOperation.Controllers;

[ApiController]
[Route("[controller]")]
public class PresignedController : ControllerBase
{
    private readonly IMinioClient _minioClient;

    public PresignedController(IMinioClient minioCLient)
    {
        _minioClient = minioCLient;
    }

    [HttpGet("PresignedGetObject")]
    public async Task<IActionResult> PresignedGetObject(string bucketName, string objectName)
    {
        try
        {
            var time = new TimeSpan(7, 0, 0, 0);
            var args = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry((int)time.TotalSeconds);
            var result = await _minioClient.PresignedGetObjectAsync(args);
            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    [HttpGet("PresignedPutObject")]
    public async Task<IActionResult> PresignedPutObject(string bucketName, string objectName)
    {
        try
        {
            var time = new TimeSpan(7, 0, 0, 0);
            var args = new PresignedPutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry((int)time.TotalSeconds);
            var result = await _minioClient.PresignedPutObjectAsync(args);
            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
        
    [HttpGet("PresignedPostObject")]
    public async Task<IActionResult> PresignedPostObject(string bucketName, string objectName)
    {
        try
        {
            var time = new TimeSpan(7, 0, 0, 0);

            var postPolicy = new PostPolicy();
            postPolicy.SetBucket(bucketName);
            postPolicy.SetKey(objectName);
            postPolicy.SetExpires(DateTime.UtcNow.Add(time));
            postPolicy.SetContentRange(1, 10 * 1024 * 1024); // File size between 1 byte and 10MB

            
            var args = new PresignedPostPolicyArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithPolicy(postPolicy);
            var result = await _minioClient.PresignedPostPolicyAsync(args);
            
            string curlCommand = "curl ";
            foreach (KeyValuePair<string, string> pair in result.Item2)
            {
                curlCommand = curlCommand + " -F " + pair.Key + "=" + pair.Value;
            }
            curlCommand = curlCommand + " -F file=@/etc/bashrc " + result.Item1.ToString();
            return Ok(curlCommand);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}