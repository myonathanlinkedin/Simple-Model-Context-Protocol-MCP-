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
        public static async Task<string> CreateNoteAsync([Description("Content of the new note")] string content)
        {
            using var context = await GetDbContextAsync();
            var note = new Note { Content = content };
            context.Notes.Add(note);
            await context.SaveChangesAsync();
            return $"Note created with ID: {note.Id}";
        }

        // Retrieve a note by ID
        [McpServerTool, Description("Retrieve a note by ID")]
        public static async Task<string> GetNoteAsync([Description("ID of the note to retrieve")] int id)
        {
            using var context = await GetDbContextAsync();
            var note = await context.Notes.FindAsync(id);
            return note != null ? $"Note ID: {note.Id}, Content: {note.Content}" : "Note not found";
        }

        // Update an existing note
        [McpServerTool, Description("Update an existing note")]
        public static async Task<string> UpdateNoteAsync([Description("ID of the note to update")] int id, [Description("New content for the note")] string newContent)
        {
            using var context = await GetDbContextAsync();
            var note = await context.Notes.FindAsync(id);
            if (note != null)
            {
                note.Content = newContent;
                context.Notes.Update(note);
                await context.SaveChangesAsync();
                return $"Note ID: {note.Id} updated successfully";
            }
            return "Note not found";
        }

        // Delete a note by ID
        [McpServerTool, Description("Delete a note by ID")]
        public static async Task<string> DeleteNoteAsync([Description("ID of the note to delete")] int id)
        {
            using var context = await GetDbContextAsync();
            var note = await context.Notes.FindAsync(id);
            if (note != null)
            {
                context.Notes.Remove(note);
                await context.SaveChangesAsync();
                return $"Note ID: {note.Id} deleted successfully";
            }
            return "Note not found";
        }

        // Retrieve all notes
        [McpServerTool, Description("Get all notes")]
        public static async Task<string> GetAllNotesAsync()
        {
            using var context = await GetDbContextAsync();
            var notes = await context.Notes.ToListAsync();
            if (notes.Any())
            {
                var noteList = string.Join(Environment.NewLine, notes.Select(note => $"Note ID: {note.Id}, Content: {note.Content}"));
                return noteList;
            }
            return "No notes found";
        }
    }
}
