using System.ComponentModel.DataAnnotations;

namespace InvoiceSystem.Application.DTOs
{
    public class EditInvoiceDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select an employee")]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Days worked is required")]
        [Display(Name = "Days Worked")]
        [Range(1, 31, ErrorMessage = "Days worked must be between 1 and 31")]
        public int DaysWorked { get; set; }
    }
} 