using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace proekt_za_6ca.ViewModels
{
    public class RestaurantCreateViewModel
    {
        [Required]
        [MaxLength(64, ErrorMessage = "Restaurant name cannot exceed 64 characters")]
        [Display(Name = "Restaurant Name")]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;
        
        [Required]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90")]
        [Display(Name = "Latitude")]
        public double Latitude { get; set; }
        
        [Required]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180")]
        [Display(Name = "Longitude")]
        public double Longitude { get; set; }
        
        [Display(Name = "Upload Image")]
        public IFormFile? ImageFile { get; set; }
        
        [Display(Name = "Or Image URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? ImageUrl { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;
    }

    public class RestaurantEditViewModel
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(64, ErrorMessage = "Restaurant name cannot exceed 64 characters")]
        [Display(Name = "Restaurant Name")]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;
        
        [Required]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90")]
        [Display(Name = "Latitude")]
        public double Latitude { get; set; }
        
        [Required]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180")]
        [Display(Name = "Longitude")]
        public double Longitude { get; set; }
        
        [Display(Name = "Upload New Image")]
        public IFormFile? ImageFile { get; set; }
        
        [Display(Name = "Or Image URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Current Image")]
        public string? CurrentImageUrl { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;
        
        public DateTime CreatedOn { get; set; }
    }
}