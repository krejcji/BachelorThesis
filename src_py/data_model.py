"""Data model for loading from Excel file.
   Author: https://github.com/Breta01/virtual-warehouse/tree/master/src/parser"""
from dataclasses import InitVar, dataclass
from typing import List

from utils import convert_dim, convert_weight
import datetime


@dataclass
class Coord:
    """Location coordinates."""

    x: float = None
    y: float = None
    z: float = None


@dataclass
class ItemUnit:
    """Basic properties of item unit."""

    conversion_qty: int
    qty_uom: str
    length: float
    width: float
    height: float
    dim_uom: InitVar[str]
    weight: float
    weight_uom: InitVar[str]

    def __post__init__(self, dim_uom, weight_uom):
        self.length = convert_dim(self.length, dim_uom)
        self.width = convert_dim(self.width, dim_uom)
        self.height = convert_dim(self.height, dim_uom)
        self.weight = convert_weight(self.weight, weight_uom)


@dataclass
class Item:
    """Basic description of item, including packaging units."""

    id: str
    description: str
    gtype: str
    zone: str
    base_unit: ItemUnit
    unit_levels: List[ItemUnit]


@dataclass
class Coordinates:
    x: int
    y: int
    z: int


@dataclass
class Location:
    """Description of location in warehouse."""

    id: str
    ltype: str
    lclass: str
    lsubclass: str
    length: float
    width: float
    height: float
    dim_uom: InitVar[str]
    max_weight: float = None
    weight_uom: InitVar[str] = None
    zone: str = None
    coord = None

    def __post__init__(self, dim_uom, weight_uom):
        self.length = convert_dim(self.length, dim_uom)
        self.width = convert_dim(self.width, dim_uom)
        self.height = convert_dim(self.height, dim_uom)
        if self.max_weight:
            self.max_weight = convert_weight(self.max_weight, weight_uom)

    def set_coord(self, coord):
        if coord is not None:
            self.coord = Coordinates(int(coord.x.value), int(coord.y.value), int(coord.z.value))


@dataclass
class Order:
    """Description of single order from warehouse."""

    id: int
    direction: str
    country: str
    delivery_date: str
    s_ship_date: str
    a_ship_date: str
    line_num: int
    item_id: str
    requested_qty: int
    total_qty: int
    qty_uom: str

    def __post__init__(self):
        self.delivery_date = datetime.strptime(self.delivery_date, "%d.%m.%Y")
        self.s_ship_date = datetime.strptime(self.s_ship_date, "%d.%m.%Y")
        self.a_ship_date = datetime.strptime(self.a_ship_date, "%d.%m.%Y")


@dataclass
class PickerOrder:
    """Description of single order from warehouse."""

    id: int
    direction: str
    country: str
    delivery_date: str
    s_ship_date: str
    a_ship_date: str
    line_num: int
    item_id: str
    requested_qty: int
    total_qty: int
    qty_uom: str
    picker: str

    def __post__init__(self):
        self.delivery_date = datetime.strptime(self.delivery_date, "%d.%m.%Y")
        self.s_ship_date = datetime.strptime(self.s_ship_date, "%d.%m.%Y")
        self.a_ship_date = datetime.strptime(self.a_ship_date, "%d.%m.%Y")


@dataclass
class Inventory:
    date: str
    location_id: str
    ltype: str
    item_id: str
    expiry_date: str
    available_qty: int
    onhand_qty: int
    transi_qty: int
    allocated_qty: int
    suspense_qty: int

    def __post__init__(self):
        self.date = datetime.strptime(self.date, "%d.%m.%Y")
