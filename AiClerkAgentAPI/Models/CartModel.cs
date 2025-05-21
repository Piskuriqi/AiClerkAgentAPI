namespace AiClerkAgentAPI.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }  
        public string? ProductName { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; } 

    }

    public class CartModel
    {
        public string? ConversationId { get; set; }
        public List<CartItem> Items { get; set; } = new();

    }
   
}
