using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SorteoSanticaza.Models
{
    public class RafflePublic
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Description { get; set; } = "";
        public string PrizeTitle { get; set; } = "";
        public string PrizeDescription { get; set; } = "";
        public string DrawAt { get; set; } = "";
        public string Status { get; set; } = "";
        public int TotalTickets { get; set; }
        public int SoldTickets { get; set; }
        public int RemainingTickets { get; set; }
        public string? VideoUrl { get; set; }
        public string? ImageUrl { get; set; }
        public List<PackagePublic> Packages { get; set; } = new List<PackagePublic>();
    }

    public class PackagePublic
    {
        public int Id { get; set; }
        public int Chances { get; set; }
        public long PriceCents { get; set; }
        public string Label { get; set; } = "";
        public bool Popular { get; set; }
    }

    public class PurchaseForm
    {
        [Required]
        public int PackageId { get; set; }

        [Required, MinLength(2), Display(Name = "Nombre")]
        public string FirstName { get; set; } = "";

        [Required, MinLength(2), Display(Name = "Apellido")]
        public string LastName { get; set; } = "";

        [Required, Display(Name = "DNI")]
        public string Dni { get; set; } = "";

        [Required, Display(Name = "Fecha de nacimiento")]
        public string BirthDate { get; set; } = "";

        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required, Display(Name = "Teléfono")]
        public string Phone { get; set; } = "";

        [Range(typeof(bool), "true", "true", ErrorMessage = "Debés aceptar los términos.")]
        public bool AcceptTerms { get; set; }
    }

    public class OrderResult
    {
        public int OrderId { get; set; }
        public string PublicId { get; set; } = "";
        public long AmountCents { get; set; }
        public int Chances { get; set; }
        public string Label { get; set; } = "";
    }

    public class CheckoutResult
    {
        public string Mode { get; set; } = "demo";
        public string CheckoutUrl { get; set; } = "";
        public List<int> Tickets { get; set; } = new List<int>();
        public string PublicId { get; set; } = "";
        public string Status { get; set; } = "";
        public int Chances { get; set; }
        public long AmountCents { get; set; }
    }

    public class MyNumbersResult
    {
        public string OrderPublicId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int Chances { get; set; }
        public long AmountCents { get; set; }
        public string? PaidAt { get; set; }
        public List<int> Numbers { get; set; } = new List<int>();
    }

    public class WinnerPublic
    {
        public int Id { get; set; }
        public string RaffleTitle { get; set; } = "";
        public int TicketNumber { get; set; }
        public string PrizeLabel { get; set; } = "";
        public string WinnerName { get; set; } = "";
        public string DrawnAt { get; set; } = "";
    }

    public class AdminOrderRow
    {
        public int Id { get; set; }
        public string PublicId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Dni { get; set; } = "";
        public string Phone { get; set; } = "";
        public int Chances { get; set; }
        public long AmountCents { get; set; }
        public string Status { get; set; } = "";
        public string? TicketNumbers { get; set; }
        public string CreatedAt { get; set; } = "";
        public string? PaidAt { get; set; }
    }

    public class PaidOrderView
    {
        public int Id { get; set; }
        public string PublicId { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Status { get; set; } = "";
        public int PackageId { get; set; }
        public int Chances { get; set; }
        public long AmountCents { get; set; }
        public string? PreferenceId { get; set; }
        public string? PaymentRef { get; set; }
        public string? PaymentMethod { get; set; }
        public string? StatusDetail { get; set; }
        public string Label { get; set; } = "Chances SANTICAZA";
        public List<int> Tickets { get; set; } = new List<int>();
    }

    public class CheckoutPageModel
    {
        public PaidOrderView Order { get; set; } = new PaidOrderView();
        public string? StatusHint { get; set; }
    }

    public class PackageAdminRow
    {
        public int Id { get; set; }
        public int Chances { get; set; } = 1;
        public decimal PriceArs { get; set; }
        public string Label { get; set; } = "";
        public bool Popular { get; set; }
        public int SortOrder { get; set; }
        public bool Active { get; set; } = true;
    }

    public class RaffleAdminForm
    {
        public int Id { get; set; }

        [Required, Display(Name = "Título"), MaxLength(200)]
        public string Title { get; set; } = "Sorteo SANTICAZA";

        [Required, Display(Name = "Subtítulo"), MaxLength(300)]
        public string Subtitle { get; set; } = "Participá y ganá.";

        [Required, Display(Name = "Descripción")]
        public string Description { get; set; } = "";

        [Required, Display(Name = "Premio"), MaxLength(300)]
        public string PrizeTitle { get; set; } = "";

        [Required, Display(Name = "Detalle del premio")]
        public string PrizeDescription { get; set; } = "";

        [Required, Display(Name = "Fecha del sorteo")]
        public string DrawAtLocal { get; set; } = "";

        [Display(Name = "Estado")]
        public string Status { get; set; } = "active";

        [Range(1, 1000000), Display(Name = "Total de chances")]
        public int TotalTickets { get; set; } = 10000;

        [Range(1, 1000000), Display(Name = "Número inicial")]
        public int TicketStart { get; set; } = 1;

        [Display(Name = "URL de video (opcional)")]
        public string? VideoUrl { get; set; }

        public string? ImageUrl { get; set; }

        public int SoldTickets { get; set; }

        public List<PackageAdminRow> Packages { get; set; } = new List<PackageAdminRow>();
    }

    public class RaffleAdminListItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string PrizeTitle { get; set; } = "";
        public string Status { get; set; } = "";
        public string DrawAt { get; set; } = "";
        public string? ImageUrl { get; set; }
        public int TotalTickets { get; set; }
        public int SoldTickets { get; set; }
        public int PackageCount { get; set; }
    }
}
