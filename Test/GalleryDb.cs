using DanilovSoft.MicroORM;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text;

namespace ConsoleTest
{
    // Таблица gallery.
    public sealed class GalleryDb
    {
        [Column("gid")]
        [SqlProperty("gid")]
        public int GalleryId { get; set; }

        [Column("token")]
        [SqlProperty("token")]
        public string GalleryToken { get; set; }

        [SqlProperty("uploader")]
        public string? UploaderName { get; set; }

        [SqlProperty("posted_date")]
        public DateTime? PostedDate { get; set; }

        [SqlProperty("title")]
        public string Title { get; set; }

        [Column("orig_title")]
        [SqlProperty("orig_title")]
        public string OriginalTitle { get; set; }

        [SqlProperty("pages")]
        public int? Pages { get; set; }

        [SqlProperty("rating")]
        public float? Rating { get; set; }

        [SqlProperty("rating_count")]
        public ushort? RatingCount { get; set; }

        [SqlProperty("category")]
        public int? Category { get; set; }

        //[SqlIgnore]
        //[SqlProperty("favorited")]
        //[Column("favorited")]
        //public int? Favorited { get; set; }

        [SqlIgnore]
        [NotMapped]
        public short? FavCat { get; set; }

        [SqlProperty("file_name")]
        private readonly Guid _fileName;

        [SqlProperty("file_extension")]
        private readonly string _fileExtension;

        /// <summary>
        /// Размер превью.
        /// </summary>
        [SqlProperty("width")]
        private readonly short _thumbWidth;

        /// <summary>
        /// Размер превью.
        /// </summary>
        [SqlProperty("height")]
        private readonly short _thumbHeight;

        [SqlProperty("parent")]
        public int? ParentGid { get; set; }

        [SqlProperty("parent_token")]
        public string ParentToken { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext _)
        {
            
        }
    }
}
