using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS_Backend.Models
{
    [Table("RefreshTokens")]
    public class RefreshToken
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }
        [Column(name: "user_id")]
        public string UserId { get; set; } // Linked to the AspNet Identity User Id
        [Column(name: "token")]
        public string Token { get; set; }
        [Column(name: "jwt_id")]
        public string JwtId { get; set; } // Map the token with jwtId
        [Column(name: "is_used")]
        public bool IsUsed { get; set; } // if its used we don't want generate a new Jwt token with the same refresh token
        [Column(name: "is_revoked")]
        public bool IsRevoked { get; set; } // if it has been revoke for security reasons
        [Column(name: "added_date")]
        public DateTime AddedDate { get; set; }
        [Column(name: "expiry_date")]
        public DateTime ExpiryDate { get; set; } // Refresh token is long lived it could last for months.

        [ForeignKey(nameof(UserId))]
        public virtual IdentityUser? User { get; set; }
    }
}
