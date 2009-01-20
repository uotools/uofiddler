using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// ascii text support written by arul
namespace Ultima
{
	public sealed class ASCIIFont
	{
        private byte m_Header;
        private byte[] m_Unk;
        private Bitmap[] m_Characters;
		private int m_Height;
		
        public byte Header { get { return m_Header; } }
        public byte[] Unk { get { return m_Unk; } set { m_Unk = value; } }
		public Bitmap[] Characters { get { return m_Characters; } set { m_Characters = value; } }
        public int Height { get { return m_Height; } set { m_Height = value; } }
        

		public ASCIIFont(byte header)
		{
            m_Header = header;
			Height		= 0;
            m_Unk = new byte[224];
			Characters	= new Bitmap[ 224 ];
		}

        /// <summary>
        /// Gets Bitmap of given character
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
		public Bitmap GetBitmap( char character )
		{
			return m_Characters[ ( ( ( ( (int)character ) - 0x20 ) & 0x7FFFFFFF ) % 224 ) ];
		}

		public int GetWidth( string text )
		{
			if( text == null || text.Length == 0 ) { return 0; }

			int width = 0;

			for( int i = 0; i < text.Length; ++i )
			{
				width += GetBitmap( text[ i ] ).Width;
			}

			return width;
		}

        public void ReplaceCharacter(int character, Bitmap import)
        {
            m_Characters[character] = import;
            m_Height = import.Height;
        }

		public static ASCIIFont GetFixed( int font )
		{
			if( font < 0 || font > 9 )
			{
				return ASCIIText.Fonts[ 3 ];
			}

			return ASCIIText.Fonts[ font ];
		}
	}

	public static class ASCIIText
	{
		public static ASCIIFont[] Fonts = new ASCIIFont[ 10 ];

		static ASCIIText()
		{
            Initialize();
		}

        /// <summary>
        /// Reads fonts.mul
        /// </summary>
        public static unsafe void Initialize()
        {
            string path = Files.GetFilePath("fonts.mul");

            if (path != null)
            {
                using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open,FileAccess.Read,FileShare.Read)))
                {
                    for (int i = 0; i < 10; ++i)
                    {
                        byte header = reader.ReadByte();
                        Fonts[i] = new ASCIIFont(header);

                        for (int k = 0; k < 224; ++k)
                        {
                            byte width = reader.ReadByte();
                            byte height = reader.ReadByte();
                            byte unk = reader.ReadByte(); // delimeter?

                            if (width > 0 && height > 0)
                            {
                                if (height > Fonts[i].Height && k < 96)
                                    Fonts[i].Height = height;

                                Bitmap bmp = new Bitmap(width, height);
                                BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555);
                                ushort* line = (ushort*)bd.Scan0;
                                int delta = bd.Stride >> 1;

                                for (int y = 0; y < height; ++y, line += delta)
                                {
                                    ushort* cur = line;
                                    for (int x = 0; x < width; ++x)
                                    {
                                        ushort pixel = (ushort)(reader.ReadByte() | (reader.ReadByte() << 8));
                                        if (pixel == 0)
                                            cur[x] = pixel;
                                        else
                                            cur[x] = (ushort)(pixel ^ 0x8000);
                                    }
                                }
                                bmp.UnlockBits(bd);
                                Fonts[i].Characters[k] = bmp;
                                Fonts[i].Unk[k] = unk;
                            }
                        }
                    }
                }
            }
        }

        public static unsafe void Save(string FileName)
        {
            using (FileStream fs = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                BinaryWriter bin = new BinaryWriter(fs);

                for (int i = 0; i < 10; ++i)
                {
                    bin.Write(Fonts[i].Header);
                    for (int k = 0; k < 224; ++k)
                    {
                        bin.Write((byte)Fonts[i].Characters[k].Width);
                        bin.Write((byte)Fonts[i].Characters[k].Height);
                        bin.Write(Fonts[i].Unk[k]);
                        Bitmap bmp = Fonts[i].Characters[k];
                        BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format16bppArgb1555);
                        ushort* line = (ushort*)bd.Scan0;
                        int delta = bd.Stride >> 1;
                        for (int y = 0; y < bmp.Height; ++y,line+=delta)
                        {
                            ushort* cur = line;
                            for (int x = 0; x < bmp.Width; ++x)
                            {
                                if (cur[x] == 0)
                                    bin.Write(cur[x]);
                                else
                                    bin.Write((ushort)(cur[x] ^ 0x8000));
                            }
                        }
                        bmp.UnlockBits(bd);
                    }
                }
            }
        }

        /// <summary>
        /// Draws Text with font in Bitmap and returns
        /// </summary>
        /// <param name="fontId"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Bitmap DrawText(int fontId, string text)
        {
            ASCIIFont font = ASCIIFont.GetFixed(fontId);
            Bitmap result = new Bitmap(font.GetWidth(text)+2, font.Height+2);

            int dx = 2;
            int dy = font.Height+2;
            using (Graphics graph = Graphics.FromImage(result))
            {
                for (int i = 0; i < text.Length; ++i)
                {
                    Bitmap bmp = font.GetBitmap(text[i]);
                    graph.DrawImage(bmp,dx,dy-bmp.Height);
                    dx += bmp.Width;
                }
            }
            return result;
        }

        // Funzt nett...

        ///// <summary>
        ///// Returns Bitmap with Text
        ///// </summary>
        ///// <param name="fontId"></param>
        ///// <param name="text"></param>
        ///// <param name="hueId"></param>
        ///// <returns></returns>
        //public unsafe static Bitmap DrawText( int fontId, string text, short hueId )
        //{
        //    ASCIIFont font = ASCIIFont.GetFixed( fontId );

        //    Bitmap result		= 
        //        new Bitmap( font.GetWidth( text ), font.Height );
        //    BitmapData surface	= 
        //        result.LockBits( new Rectangle( 0, 0, result.Width, result.Height ), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb );

        //    int dx				= 0;

        //    for( int i = 0; i < text.Length; ++i )
        //    {
        //        Bitmap bmp		=
        //            font.GetBitmap( text[ i ] );				
        //        BitmapData chr	= 
        //            bmp.LockBits( new Rectangle( 0, 0, bmp.Width, bmp.Height ), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb );

        //        for( int dy = 0; dy < chr.Height; ++dy )
        //        {
        //            int* src	= 
        //                ( (int*)chr.Scan0 ) + ( chr.Stride * dy );
        //            int* dest  = 
        //                ( ( (int*)surface.Scan0 ) + ( surface.Stride * ( dy + ( font.Height - chr.Height ) ) ) ) + ( dx << 2 );

        //            for( int k = 0; k < chr.Width; ++k )
        //                *dest++ = *src++;
					
        //        }

        //        dx += chr.Width;
        //        bmp.UnlockBits( chr );
        //    }

        //    result.UnlockBits( surface );

        //    hueId = (short)(( hueId & 0x3FFF ) - 1);
        //    if( hueId >= 0 && hueId < Hues.List.Length )
        //    {
        //        Hue hueObject = Hues.List[ hueId ];

        //        if( hueObject != null )
        //            hueObject.ApplyTo( result, ( ( hueId & 0x8000 ) == 0 ) );
        //    }

        //    return result;
        //}
	}
}
