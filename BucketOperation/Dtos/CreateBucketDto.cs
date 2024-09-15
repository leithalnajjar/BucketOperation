using System.ComponentModel.DataAnnotations;
using Minio.DataModel.ObjectLock;

namespace BucketOperation.Dtos;

public class CreateBucketDto
{
    [Required(ErrorMessage = "Bucket name is required.")]
    [MinLength(3, ErrorMessage = "Bucket name must be at least 3 characters long.")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Bucket name must not contain uppercase letters and can only include lowercase letters, numbers, and hyphens.")]
    public string Name { get; set; }

    public bool Versioning { get; set; }
    public bool ObjectLocking { get; set; }
    public ObjectRetentionMode? Mode { get; set; }
    public int? NumOfDays { get; set; }
}