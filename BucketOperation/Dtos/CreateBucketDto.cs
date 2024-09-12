using System.ComponentModel.DataAnnotations;

namespace BucketOperation.Dtos;

public class CreateBucketDto
{
    [Required(ErrorMessage = "Bucket name is required.")]
    [MinLength(3, ErrorMessage = "Bucket name must be at least 3 characters long.")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Bucket name must not contain uppercase letters and can only include lowercase letters, numbers, and hyphens.")]
    public string Name { get; set; }
}