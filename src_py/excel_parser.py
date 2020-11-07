"""Parser of Excel data files."""
import io
from pathlib import Path

from data_model import *
from xlrd import open_workbook


def parse_document(data_path):
    document = open_workbook(data_path)

    # Parse LOCATIONmaster sheet
    sheet = document.sheet_by_name("LOCATIONmaster")
    locations = {}
    for row in range(1, sheet.nrows):
        location_id = sheet.cell(row, 0).value
        locations[location_id] = Location(
            *(sheet.cell(row, i).value for i in range(11))
        )

    # Parse XYZ_coordinates sheet
    sheet = document.sheet_by_name("XYZ_coordinates")
    for row in range(1, sheet.nrows):
        location_id = sheet.cell(row, 0).value
        coord = Coord(*(sheet.cell(row, i) for i in range(1, 4)))
        locations[location_id].set_coord(coord)

    # Parse ITEMmaster sheet
    sheet = document.sheet_by_name("ITEMmaster")
    items = {}
    for row in range(1, sheet.nrows):
        item_id, description, gtype, zone = (sheet.cell(row, i).value for i in range(4))
        unit_levels = []
        for col in range(4, sheet.ncols, 8):
            unit_levels.append(
                ItemUnit(*(sheet.cell(row, col + i).value for i in range(8)))
            )
        items[item_id] = Item(
            item_id, description, gtype, zone, unit_levels[0], unit_levels
        )

    # Parse Inventory Balance sheet  ('balance' in final version, most likely)
    sheet = document.sheet_by_name("Inventory Ballance")
    balance = {}
    for row in range(1, sheet.nrows):
        date, location_id = sheet.cell(row, 0).value, sheet.cell(row, 1).value
        if not date in balance:
            balance[date] = {}
        balance[date][location_id] = Inventory(
            *(sheet.cell(row, i).value for i in range(10))
        )

    # Parse Order sheet - 12 columns with the PICKER column
    sheet = document.sheet_by_name("Order")
    orders = []
    for row in range(1, sheet.nrows):
        orders.append(PickerOrder(*(sheet.cell(row, i).value for i in range(12))))

    return locations, items, balance, orders


def serialize_warehouse(parsed_warehouse, file_path):
    locations = parsed_warehouse[0]
    items = parsed_warehouse[1]
    balance = parsed_warehouse[2]
    orders = parsed_warehouse[3]

    file = io.open(file_path, "w+")

    # Compute the warehouse dimension
    x_max = 0
    y_max = 0
    z_max = 0

    for location in locations.values():
        if location.coord is None:
            continue
        if location.coord.x > x_max:
            x_max = location.coord.x
        if location.coord.y > y_max:
            y_max = location.coord.y
        if location.coord.z > z_max:
            z_max = location.coord.z
    file.write(f"Dimension: {x_max},{y_max},{z_max}\n")

    file.write("LOCATIONmaster\n")
    file.write("x_coord,y_coord,z_coord,id,lclass,lsubclass,ltype,zone\n")
    for location in locations.values():
        if location.coord is None:
            continue
        file.write(f"{location.coord.x},{location.coord.y},{location.coord.z}"\
                   f",{location.id},{location.lclass},{location.lsubclass}"\
                   f",{location.ltype},{location.zone}\n"\
                   )

    file.write("\n")
    file.write("ITEMmaster\n")
    file.write("description,id,zone\n")
    for item in items.values():
        file.write(f"{item.description},{item.id},{item.zone}\n")

    file.write("\n")
    file.write("Inventory balance\n")
    file.write("location_id,item_id,available_qty\n")
    for date in balance.keys():
        file.write(f"*date:{date}\n")
        for inventory_record in balance[date].values():
            file.write(f"{inventory_record.location_id},{inventory_record.item_id}"\
                       f",{int(inventory_record.available_qty)}\n"
                       )

    file.write("\n")
    file.write("Orders\n")
    file.write("order_id,order_line,direction,item_id,requested_qty,picker\n")
    for order in orders:
        file.write(f"{int(order.id)},{int(order.line_num)},{order.direction},{order.item_id},{int(order.requested_qty)},{order.picker}\n")
    file.close()
    print()


if __name__ == "__main__":
    data_file = Path(__file__).parent.joinpath("warehouse_no_1.xlsx")
    document = parse_document(data_file)
    serialize_warehouse(document, "test_warehouse.txt")
    print()
