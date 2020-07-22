from xlrd import open_workbook

document = open_workbook('warehouse_data.xlsx')

# Parse XYZ_coordinates sheet
sheet = document.sheet_by_name("XYZ_coordinates")
locations_coord = {}

for row in range(1, sheet.nrows):
    locations_coord[sheet.cell(row, 0).value] = (int(sheet.cell(row, 1).value),  # X
                                                 int(sheet.cell(row, 2).value),  # Y
                                                 int(sheet.cell(row, 3).value))  # Z

# Parse LOCATIONmaster sheet
sheet = document.sheet_by_name("LOCATIONmaster")
locations_attr = {}

for row in range(1, sheet.nrows):
    attributes = []
    for col in range(1, sheet.ncols):
        attributes.append(sheet.cell(row, col).value)
    locations_attr[sheet.cell(row, 0).value] = attributes

# Parse ITEMmaster sheet
sheet = document.sheet_by_name("ITEMmaster")
items = {}

for row in range(1, sheet.nrows):
    item_attr = []
    for col in range(1, sheet.ncols):
        item_attr.append(sheet.cell(row, col).value)
    items[sheet.cell(row, 0).value] = item_attr

# Parse Inventory Ballance sheet  ('balance' in final version, most likely)
sheet = document.sheet_by_name("Inventory Ballance")
balance = []

for row in range(1, sheet.nrows):
    row_attr = []
    for col in range(0, sheet.ncols):
        row_attr.append(sheet.cell(row, col).value)
    balance.append(row_attr)

# Parse Order sheet
sheet = document.sheet_by_name("Order")
orders = {}

for row in range(1, sheet.nrows):
    order = []
    for col in range(1, sheet.ncols):
        order.append(sheet.cell(row, col).value)
    if sheet.cell(row, 0).value not in orders:
        orders[sheet.cell(row, 0).value] = [order]
    else:
        orders[sheet.cell(row, 0).value].append(order)
