using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MCPServer.Models;

namespace MCPServer.Tools
{
    [McpServerToolType]
    public sealed class APITools
    {
        // Private helper method for getting DbContext
        private static async Task<AppDbContext> GetDbContextAsync()
        {
            var context = new AppDbContext();
            await context.Database.EnsureCreatedAsync();
            return context;
        }

        // Existing method
        [McpServerTool, Description("Get echo with random seed")]
        public static async Task<string> GetEchoRandomSeed([Description("Echo from the server with random seed")] string paramInput)
        {
            // Generate a random string to use as a seed
            Random random = new Random();
            int randomSeed = random.Next();
            string seedString = randomSeed.ToString(CultureInfo.InvariantCulture);

            // Hash the random seed using SHA-256
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(seedString));

                // Convert the hash bytes to a hex string
                string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                // Return the formatted string with Echo and the hashed seed
                return await Task.FromResult($"Echo MCPServer: {paramInput}, here is the seed: {hashString}");
            }
        }

        // Create a new note
        [McpServerTool, Description("Create a new note")]
        public static async Task<Note?> CreateNoteAsync([Description("Content of the new note")] string content)
        {
            using var context = await GetDbContextAsync();
            var note = new Note { Content = content };
            context.Notes.Add(note);
            await context.SaveChangesAsync();
            return note;
        }

        // Retrieve a note by ID
        [McpServerTool, Description("Retrieve a note by ID")]
        public static async Task<Note?> GetNoteAsync([Description("ID of the note to retrieve")] int id)
        {
            using var context = await GetDbContextAsync();
            return await context.Notes.FindAsync(id);
        }

        // Update an existing note
        [McpServerTool, Description("Update an existing note")]
        public static async Task<Note?> UpdateNoteAsync(
            [Description("ID of the note to update")] int id,
            [Description("New content for the note")] string newContent)
        {
            using var context = await GetDbContextAsync();
            var note = await context.Notes.FindAsync(id);
            if (note != null)
            {
                note.Content = newContent;
                context.Notes.Update(note);
                await context.SaveChangesAsync();
            }
            return note;
        }

        // Delete a note by ID
        [McpServerTool, Description("Delete a note by ID")]
        public static async Task<Note?> DeleteNoteAsync([Description("ID of the note to delete")] int id)
        {
            using var context = await GetDbContextAsync();
            var note = await context.Notes.FindAsync(id);
            if (note != null)
            {
                context.Notes.Remove(note);
                await context.SaveChangesAsync();
            }
            return note;
        }

        // Retrieve all notes
        [McpServerTool, Description("Get all notes")]
        public static async Task<List<Note>> GetAllNotesAsync()
        {
            using var context = await GetDbContextAsync();
            return await context.Notes.ToListAsync();
        }
    }
}
