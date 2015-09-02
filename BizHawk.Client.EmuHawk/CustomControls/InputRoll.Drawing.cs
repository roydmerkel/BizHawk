﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class InputRoll
	{
		protected override void OnPaint(PaintEventArgs e)
		{
			using (var LCK = Gdi.LockGraphics(e.Graphics))
			{
				Gdi.StartOffScreenBitmap(Width, Height);

				//White Background
				Gdi.SetBrush(Color.White);
				Gdi.SetSolidPen(Color.White);
				Gdi.FillRectangle(0, 0, Width, Height);

				// Lag frame calculations
				SetLagFramesArray();

				if (_columns.VisibleColumns.Any())
				{
					DrawColumnBg(e);
					DrawColumnText(e);
				}

				//Background
				DrawBg(e);

				//Foreground
				DrawData(e);

				DrawColumnDrag(e);
				DrawCellDrag(e);

				Gdi.CopyToScreen();
				Gdi.EndOffScreenBitmap();
			}
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			// Do nothing, and this should never be called
		}

		private void DrawColumnDrag(PaintEventArgs e)
		{
			if (_columnDown != null && _currentX.HasValue && _currentY.HasValue && IsHoveringOnColumnCell)
			{
				int x1 = _currentX.Value - (_columnDown.Width.Value / 2);
				int y1 = _currentY.Value - (CellHeight / 2);
				int x2 = x1 + _columnDown.Width.Value;
				int y2 = y1 + CellHeight;

				Gdi.SetSolidPen(this.BackColor);
				Gdi.DrawRectangle(x1, y1, x2, y2);
				Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
				Gdi.DrawString(_columnDown.Text, new Point(x1 + CellWidthPadding, y1 + CellHeightPadding));
			}
		}

		private void DrawCellDrag(PaintEventArgs e)
		{
			if (DraggingCell != null)
			{
				var text = "";
				int offsetX = 0;
				int offsetY = 0;
				if (QueryItemText != null)
				{
					QueryItemText(DraggingCell.RowIndex.Value, DraggingCell.Column, out text, ref offsetX, ref offsetY);
				}

				Color bgColor = this.BackColor;
				if (QueryItemBkColor != null)
				{
					QueryItemBkColor(DraggingCell.RowIndex.Value, DraggingCell.Column, ref bgColor);
				}

				int x1 = _currentX.Value - (DraggingCell.Column.Width.Value / 2);
				int y1 = _currentY.Value - (CellHeight / 2);
				int x2 = x1 + DraggingCell.Column.Width.Value;
				int y2 = y1 + CellHeight;


				Gdi.SetBrush(bgColor);
				Gdi.FillRectangle(x1, y1, x2 - x1, y2 - y1);
				Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
				Gdi.DrawString(text, new Point(x1 + CellWidthPadding + offsetX, y1 + CellHeightPadding + offsetY));
			}
		}

		private void DrawColumnText(PaintEventArgs e)
		{
			var columns = _columns.VisibleColumns.ToList();

			if (HorizontalOrientation)
			{
				int start = -VBar.Value;

				Gdi.PrepDrawString(this.RotatedFont, this.ForeColor);

				foreach (var column in columns)
				{
					var point = new Point(CellWidthPadding, start + CellHeightPadding);

					if (IsHoveringOnColumnCell && column == CurrentCell.Column)
					{
						Gdi.PrepDrawString(this.NormalFont, SystemColors.HighlightText);
						Gdi.DrawString(column.Text, point);
						Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
					}
					else
					{
						Gdi.DrawString(column.Text, point);
					}

					start += CellHeight;
				}
			}
			else
			{
				//zeromus test
				//Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
				Gdi.PrepDrawString(this.RotatedFont, this.ForeColor);

				foreach (var column in columns)
				{
					var point = new Point(column.Left.Value + 2 * CellWidthPadding - HBar.Value, CellHeightPadding); // TODO: fix this CellPadding issue (2 * CellPadding vs just CellPadding)

					if (IsHoveringOnColumnCell && column == CurrentCell.Column)
					{
						//zeromus test
						//Gdi.PrepDrawString(this.NormalFont, SystemColors.HighlightText);
						Gdi.PrepDrawString(this.RotatedFont, SystemColors.HighlightText);
						Gdi.DrawString(column.Text, point);
						//zeromus test
						//Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
						Gdi.PrepDrawString(this.RotatedFont, this.ForeColor);
					}
					else
					{
						Gdi.DrawString(column.Text, point);
					}
				}
			}
		}

		private void DrawData(PaintEventArgs e)
		{
			List<RollColumn> columns = _columns.VisibleColumns.ToList();
			if (QueryItemText != null)
			{
				if (HorizontalOrientation)
				{
					int startRow = FirstVisibleRow;
					int range = Math.Min(LastVisibleRow, RowCount - 1) - startRow + 1;

					Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
					for (int i = 0, f = 0; f < range; i++, f++)
					{
						f += lagFrames[i];
						int LastVisible = LastVisibleColumnIndex;
						for (int j = FirstVisibleColumn; j <= LastVisible; j++)
						{
							Bitmap image = null;
							int x = 0;
							int y = 0;
							int bitmapOffsetX = 0;
							int bitmapOffsetY = 0;

							if (QueryItemIcon != null)
							{
								QueryItemIcon(f + startRow, columns[j], ref image, ref bitmapOffsetX, ref bitmapOffsetY);
							}

							if (image != null)
							{
								x = RowsToPixels(i) + CellWidthPadding + bitmapOffsetX;
								y = (j * CellHeight) + (CellHeightPadding * 2) + bitmapOffsetY;
								Gdi.DrawBitmap(image, new Point(x, y), true);
							}
							//else
							//{
							string text;
							int strOffsetX = 0;
							int strOffsetY = 0;
							QueryItemText(f + startRow, columns[j], out text, ref strOffsetX, ref strOffsetY);

							// Center Text
							x = RowsToPixels(i) + (CellWidth - text.Length * _charSize.Width) / 2;
							y = (j * CellHeight) + CellHeightPadding - VBar.Value;
							var point = new Point(x + strOffsetX, y + strOffsetY);

							var rePrep = false;
							if (SelectedItems.Contains(new Cell { Column = columns[j], RowIndex = i + startRow }))
							{
								Gdi.PrepDrawString(this.NormalFont, SystemColors.HighlightText);
								rePrep = true;
							}


							if (!string.IsNullOrWhiteSpace(text))
							{
								Gdi.DrawString(text, point);
							}

							if (rePrep)
							{
								Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
							}
							//}
						}
					}
				}
				else
				{
					int startRow = FirstVisibleRow;
					int range = Math.Min(LastVisibleRow, RowCount - 1) - startRow + 1;

					Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
					int xPadding = CellWidthPadding + 1 - HBar.Value;
					for (int i = 0, f = 0; f < range; i++, f++) // Vertical
					{
						f += lagFrames[i];
						int LastVisible = LastVisibleColumnIndex;
						for (int j = FirstVisibleColumn; j <= LastVisible; j++) // Horizontal
						{
							RollColumn col = columns[j];

							string text;
							int strOffsetX = 0;
							int strOffsetY = 0;
							Point point = new Point(col.Left.Value + xPadding, RowsToPixels(i) + CellHeightPadding);

							Bitmap image = null;
							int bitmapOffsetX = 0;
							int bitmapOffsetY = 0;

							if (QueryItemIcon != null)
							{
								QueryItemIcon(f + startRow, columns[j], ref image, ref bitmapOffsetX, ref bitmapOffsetY);
							}

							if (image != null)
							{
								Gdi.DrawBitmap(image, new Point(point.X + bitmapOffsetX, point.Y + bitmapOffsetY + CellHeightPadding), true);
							}

							QueryItemText(f + startRow, columns[j], out text, ref strOffsetX, ref strOffsetY);

							bool rePrep = false;
							if (SelectedItems.Contains(new Cell { Column = columns[j], RowIndex = f + startRow }))
							{
								Gdi.PrepDrawString(this.NormalFont, SystemColors.HighlightText);
								rePrep = true;
							}

							if (!string.IsNullOrWhiteSpace(text))
							{
								Gdi.DrawString(text, new Point(point.X + strOffsetX, point.Y + strOffsetY));
							}

							if (rePrep)
							{
								Gdi.PrepDrawString(this.NormalFont, this.ForeColor);
							}
						}
					}
				}
			}
		}

		private void DrawColumnBg(PaintEventArgs e)
		{
			Gdi.SetBrush(SystemColors.ControlLight);
			Gdi.SetSolidPen(Color.Black);

			var columns = _columns.VisibleColumns.ToList();

			if (HorizontalOrientation)
			{
				Gdi.FillRectangle(0, 0, ColumnWidth + 1, DrawHeight + 1);
				Gdi.Line(0, 0, 0, columns.Count * CellHeight + 1);
				Gdi.Line(ColumnWidth, 0, ColumnWidth, columns.Count * CellHeight + 1);

				int start = -VBar.Value;
				foreach (var column in columns)
				{
					Gdi.Line(1, start, ColumnWidth, start);
					start += CellHeight;
				}

				if (columns.Any())
				{
					Gdi.Line(1, start, ColumnWidth, start);
				}
			}
			else
			{
				int bottomEdge = RowsToPixels(0);

				// Gray column box and black line underneath
				Gdi.FillRectangle(0, 0, Width + 1, bottomEdge + 1);
				Gdi.Line(0, 0, TotalColWidth.Value + 1, 0);
				Gdi.Line(0, bottomEdge, TotalColWidth.Value + 1, bottomEdge);

				// Vertical black seperators
				for (int i = 0; i < columns.Count; i++)
				{
					int pos = columns[i].Left.Value - HBar.Value;
					Gdi.Line(pos, 0, pos, bottomEdge);
				}

				// Draw right most line
				if (columns.Any())
				{
					int right = TotalColWidth.Value - HBar.Value;
					Gdi.Line(right, 0, right, bottomEdge);
				}
			}

			// Emphasis
			foreach (var column in columns.Where(c => c.Emphasis))
			{
				Gdi.SetBrush(SystemColors.ActiveBorder);
				if (HorizontalOrientation)
				{
					Gdi.FillRectangle(1, columns.IndexOf(column) * CellHeight + 1, ColumnWidth - 1, ColumnHeight - 1);
				}
				else
				{
					Gdi.FillRectangle(column.Left.Value + 1 - HBar.Value, 1, column.Width.Value - 1, ColumnHeight - 1);
				}
			}

			// If the user is hovering over a column
			if (IsHoveringOnColumnCell)
			{
				if (HorizontalOrientation)
				{
					for (int i = 0; i < columns.Count; i++)
					{
						if (columns[i] != CurrentCell.Column)
						{
							continue;
						}

						if (CurrentCell.Column.Emphasis)
						{
							Gdi.SetBrush(Add(SystemColors.Highlight, 0x00222222));
						}
						else
						{
							Gdi.SetBrush(SystemColors.Highlight);
						}

						Gdi.FillRectangle(1, i * CellHeight + 1, ColumnWidth - 1, ColumnHeight - 1);
					}
				}
				else
				{
					//TODO multiple selected columns
					for (int i = 0; i < columns.Count; i++)
					{
						if (columns[i] == CurrentCell.Column)
						{
							//Left of column is to the right of the viewable area or right of column is to the left of the viewable area
							if (columns[i].Left.Value - HBar.Value > Width || columns[i].Right.Value - HBar.Value < 0)
							{
								continue;
							}
							int left = columns[i].Left.Value - HBar.Value;
							int width = columns[i].Right.Value - HBar.Value - left;

							if (CurrentCell.Column.Emphasis)
							{
								Gdi.SetBrush(Add(SystemColors.Highlight, 0x00550000));
							}
							else
							{
								Gdi.SetBrush(SystemColors.Highlight);
							}

							Gdi.FillRectangle(left + 1, 1, width - 1, ColumnHeight - 1);
						}
					}
				}
			}
		}

		// TODO refactor this and DoBackGroundCallback functions.
		/// <summary>
		/// Draw Gridlines and background colors using QueryItemBkColor.
		/// </summary>
		/// <param name="e"></param>
		private void DrawBg(PaintEventArgs e)
		{
			if (UseCustomBackground && QueryItemBkColor != null)
				DoBackGroundCallback(e);

			if (GridLines)
			{
				var columns = _columns.VisibleColumns.ToList();

				Gdi.SetSolidPen(SystemColors.ControlLight);
				if (HorizontalOrientation)
				{
					// Columns
					for (int i = 1; i < VisibleRows + 1; i++)
					{
						int x = RowsToPixels(i);
						Gdi.Line(x, 1, x, DrawHeight);
					}

					// Rows
					for (int i = 0; i < columns.Count + 1; i++)
					{
						Gdi.Line(RowsToPixels(0) + 1, i * CellHeight - VBar.Value, DrawWidth, i * CellHeight - VBar.Value);
					}
				}
				else
				{
					// Columns
					int y = ColumnHeight + 1;
					foreach (var column in columns)
					{
						int x = column.Left.Value - HBar.Value;
						Gdi.Line(x, y, x, Height - 1);
					}

					if (columns.Any())
					{
						Gdi.Line(TotalColWidth.Value - HBar.Value, y, TotalColWidth.Value - HBar.Value, Height - 1);
					}

					// Rows
					for (int i = 1; i < VisibleRows + 1; i++)
					{
						Gdi.Line(0, RowsToPixels(i), Width + 1, RowsToPixels(i));
					}
				}
			}

			if (SelectedItems.Any())
			{
				DoSelectionBG(e);
			}
		}

		private void DoSelectionBG(PaintEventArgs e)
		{
			// SuuperW: This allows user to see other colors in selected frames.
			Color rowColor = Color.White;
			int lastRow = -1;
			foreach (Cell cell in SelectedItems)
			{
				if (cell.RowIndex > LastVisibleRow || cell.RowIndex < FirstVisibleRow)
					continue;

				Cell relativeCell = new Cell
				{
					RowIndex = cell.RowIndex - FirstVisibleRow,
					Column = cell.Column,
				};
				relativeCell.RowIndex -= CountLagFramesAbsolute(relativeCell.RowIndex.Value);

				if (QueryRowBkColor != null && lastRow != cell.RowIndex.Value)
				{
					QueryRowBkColor(cell.RowIndex.Value, ref rowColor);
					lastRow = cell.RowIndex.Value;
				}

				Color cellColor = rowColor;
				QueryItemBkColor(cell.RowIndex.Value, cell.Column, ref cellColor);
				// Alpha layering for cell before selection
				float alpha = (float)cellColor.A / 255;
				if (cellColor.A != 255 && cellColor.A != 0)
				{
					cellColor = Color.FromArgb(rowColor.R - (int)((rowColor.R - cellColor.R) * alpha),
						rowColor.G - (int)((rowColor.G - cellColor.G) * alpha),
						rowColor.B - (int)((rowColor.B - cellColor.B) * alpha));
				}
				// Alpha layering for selection
				alpha = 0.33f;
				cellColor = Color.FromArgb(cellColor.R - (int)((cellColor.R - SystemColors.Highlight.R) * alpha),
					cellColor.G - (int)((cellColor.G - SystemColors.Highlight.G) * alpha),
					cellColor.B - (int)((cellColor.B - SystemColors.Highlight.B) * alpha));
				DrawCellBG(cellColor, relativeCell);
			}
		}

		/// <summary>
		/// Given a cell with rowindex inbetween 0 and VisibleRows, it draws the background color specified. Do not call with absolute rowindices.
		/// </summary>
		private void DrawCellBG(Color color, Cell cell)
		{
			var columns = _columns.VisibleColumns.ToList();

			int x, y, w, h;

			if (HorizontalOrientation)
			{
				x = RowsToPixels(cell.RowIndex.Value) + 1;
				w = CellWidth - 1;
				y = (CellHeight * columns.IndexOf(cell.Column)) + 1 - VBar.Value; // We can't draw without row and column, so assume they exist and fail catastrophically if they don't
				h = CellHeight - 1;
				if (x < ColumnWidth) { return; }
			}
			else
			{
				w = cell.Column.Width.Value - 1;
				x = cell.Column.Left.Value - HBar.Value + 1;
				y = RowsToPixels(cell.RowIndex.Value) + 1; // We can't draw without row and column, so assume they exist and fail catastrophically if they don't
				h = CellHeight - 1;
				if (y < ColumnHeight)
				{
					return;
				}
			}

			if (x > DrawWidth || y > DrawHeight)
			{
				return;
			} // Don't draw if off screen.

			Gdi.SetBrush(color);
			Gdi.FillRectangle(x, y, w, h);
		}

		/// <summary>
		/// Calls QueryItemBkColor callback for all visible cells and fills in the background of those cells.
		/// </summary>
		/// <param name="e"></param>
		private void DoBackGroundCallback(PaintEventArgs e)
		{
			List<RollColumn> columns = _columns.VisibleColumns.ToList();

			if (HorizontalOrientation)
			{
				int startIndex = FirstVisibleRow;
				int range = Math.Min(LastVisibleRow, RowCount - 1) - startIndex + 1;

				for (int i = 0, f = 0; f < range; i++, f++)
				{
					f += lagFrames[i];
					int LastVisible = LastVisibleColumnIndex;
					Color rowColor = Color.White;
					if (QueryRowBkColor != null)
						QueryRowBkColor(f + startIndex, ref rowColor);
					for (int j = FirstVisibleColumn; j <= LastVisible; j++) // TODO: Don't query all columns
					{
						Color itemColor = Color.White;
						QueryItemBkColor(f + startIndex, columns[j], ref itemColor);
						if (itemColor == Color.White)
							itemColor = rowColor;
						else if (itemColor.A != 255 && itemColor.A != 0)
						{
							float alpha = (float)itemColor.A / 255;
							itemColor = Color.FromArgb(rowColor.R - (int)((rowColor.R - itemColor.R) * alpha),
								rowColor.G - (int)((rowColor.G - itemColor.G) * alpha),
								rowColor.B - (int)((rowColor.B - itemColor.B) * alpha));
						}

						if (itemColor != Color.White) // An easy optimization, don't draw unless the user specified something other than the default
						{
							var cell = new Cell()
							{
								Column = columns[j],
								RowIndex = i
							};
							DrawCellBG(itemColor, cell);
						}
					}
				}
			}
			else
			{
				int startRow = FirstVisibleRow;
				int range = Math.Min(LastVisibleRow, RowCount - 1) - startRow + 1;

				for (int i = 0, f = 0; f < range; i++, f++) // Vertical
				{
					f += lagFrames[i];
					int LastVisible = LastVisibleColumnIndex;
					Color rowColor = Color.White;
					if (QueryRowBkColor != null)
						QueryRowBkColor(f + startRow, ref rowColor);
					for (int j = FirstVisibleColumn; j <= LastVisible; j++) // Horizontal
					{
						Color itemColor = Color.White;
						QueryItemBkColor(f + startRow, columns[j], ref itemColor);
						if (itemColor == Color.White)
							itemColor = rowColor;
						else if (itemColor.A != 255 && itemColor.A != 0)
						{
							float alpha = (float)itemColor.A / 255;
							itemColor = Color.FromArgb(rowColor.R - (int)((rowColor.R - itemColor.R) * alpha),
								rowColor.G - (int)((rowColor.G - itemColor.G) * alpha),
								rowColor.B - (int)((rowColor.B - itemColor.B) * alpha));
						}

						if (itemColor != Color.White) // An easy optimization, don't draw unless the user specified something other than the default
						{
							var cell = new Cell
							{
								Column = columns[j],
								RowIndex = i
							};
							DrawCellBG(itemColor, cell);
						}
					}
				}
			}
		}
	}
}
