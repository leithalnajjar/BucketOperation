using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;

namespace BucketOperation.Controllers;

[ApiController]
[Route("[controller]")]
public class ObjectController : ControllerBase
{
    private readonly IMinioClient _minioClient;

    public ObjectController(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

    [HttpPost("PubObject")]
    public async Task<IActionResult> PutObject(string bucketName, IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var args = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(file.FileName)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType)
                .WithStreamData(stream); 
            await _minioClient.PutObjectAsync(args);
            return Ok("uploaded successfully");
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    [HttpGet("GetObject")]
    public async Task<IActionResult> GetObject(string bucketName, string objectName)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            
            var args = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(async (stream) =>
                { 
                    await stream.CopyToAsync(memoryStream);
                });
            var result = await _minioClient.GetObjectAsync(args);

            memoryStream.Seek(0, SeekOrigin.Begin);
            
            return File(memoryStream, result.ContentType, result.ObjectName);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
        
    [HttpGet("GetObjectBase64")]
    public async Task<IActionResult> GetObjectBase64(string bucketName, string objectName)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            
            var args = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(async (stream) =>
                { 
                    await stream.CopyToAsync(memoryStream);
                });
            var result = await _minioClient.GetObjectAsync(args);

            memoryStream.Seek(0, SeekOrigin.Begin);

            var base64 = Convert.ToBase64String(memoryStream.ToArray());
            
            return Ok(base64);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}