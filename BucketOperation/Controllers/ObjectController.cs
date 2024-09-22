using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Tags;

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
            using (var memoryStream = new MemoryStream()) {
                var args = new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithCallbackStream(async (stream) => { await stream.CopyToAsync(memoryStream); });
                var result = await _minioClient.GetObjectAsync(args);

                memoryStream.Seek(0, SeekOrigin.Begin);

                return File(memoryStream, result.ContentType, result.ObjectName);
            }
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

    [HttpPost("CopyObject")]
    public async Task<IActionResult> CopyObject(string bucketName, string objectName, string sourceBucketName, string sourceObjectName)
    {
        try
        {
            if (objectName == sourceObjectName)
            {
                return BadRequest("Please change object name");
            }
            var copyArgs = new CopySourceObjectArgs()
                .WithBucket(sourceBucketName)
                .WithObject(sourceObjectName);
            var args = new CopyObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCopyObjectSource(copyArgs);
            await _minioClient.CopyObjectAsync(args);
            return Ok("Copied successfully");
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("StatObject")]
    public async Task<IActionResult> StatObject(string bucketName, string objectName)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var result = await _minioClient.StatObjectAsync(args);
            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("RemoveObject")]
    public async Task<IActionResult> RemoveObject(string bucketName, string objectName)
    {
        try
        {
            var args = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            await _minioClient.RemoveObjectAsync(args);
            return Ok("Removed Successfully");
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        } 
    }
    
    [HttpDelete("RemoveObjects")]
    public async Task<IActionResult> RemoveObjects(string bucketName, List<string> objectNames)
    {
        try
        {
            var args = new RemoveObjectsArgs()
                .WithBucket(bucketName)
                .WithObjects(objectNames);
            await _minioClient.RemoveObjectsAsync(args);
            return Ok("Removed Successfully");
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        } 
    }
    
    [HttpPost("SetLegalHold")]
    public async Task<IActionResult> SetLegalHold(string bucketName, string objectName, bool status)
    {
        try
        {
            var args = new SetObjectLegalHoldArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithLegalHold(status);
            await _minioClient.SetObjectLegalHoldAsync(args);
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    [HttpGet("GetLegalHold")]
    public async Task<IActionResult> GetLegalHold(string bucketName, string objectName)
    {
        try
        {
            var args = new GetObjectLegalHoldArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var result = await _minioClient.GetObjectLegalHoldAsync(args);
            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    [HttpPost("SetObjectRetention")]
    public async Task<IActionResult> SetObjectRetention(string bucketName, string objectName, ObjectRetentionMode mode, DateTime  retentionUntilDate)
    {
        try
        {
            var args = new SetObjectRetentionArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithRetentionMode(mode)
                .WithRetentionUntilDate(retentionUntilDate);
            await _minioClient.SetObjectRetentionAsync(args);
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("SetObjectTags")]
    public async Task<IActionResult> SetObjectTags(string bucketName, string objectName,[FromBody] IDictionary<string, string> tags)
    {
        try
        {
            var tagging = new Tagging(tags, true);
            var args = new SetObjectTagsArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithTagging(tagging);
            await _minioClient.SetObjectTagsAsync(args);
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    [HttpGet("GetObjectTags")]
    public async Task<IActionResult> GetObjectTags(string bucketName, string objectName)
    {
        try
        {
            var args = new GetObjectTagsArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            var tagging = await _minioClient.GetObjectTagsAsync(args);
            return Ok(tagging.Tags);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    [HttpDelete("RemoveObjectTags")]
    public async Task<IActionResult> RemoveObjectTags(string bucketName, string objectName)
    {
        try
        {
            var args = new RemoveObjectTagsArgs()
                .WithBucket(bucketName)
                .WithObject(objectName); 
            await _minioClient.RemoveObjectTagsAsync(args);
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}