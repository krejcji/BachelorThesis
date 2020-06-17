import sys
import os
import io
import os.path
import random

import numpy as np
import networkx as nx
import matplotlib.pyplot as plt

# General parameters
WALK_SPEED = 120         # cm/sec
PICK_SPEED = (15, 20, 30, 35, 45)      # sec
AISLE_DIST = 360         # cm
PICK_LOC_DIST = 120      # cm

# Warehouse specification - (type 0, type 1, type 3)
AISLES = (5, 10, 50)
STORAGE_COLUMNS = (AISLES[0] * 2, AISLES[1] * 2, AISLES[2] * 2)
ITEMS_IN_BLOCK = (10, 25, 25)
CROSS_AISLES = (2, 3, 5)
HEIGHT = (5, 5, 5)
ITEMS_IN_AISLE = (ITEMS_IN_BLOCK[0]*(CROSS_AISLES[0]-1),
                  ITEMS_IN_BLOCK[1]*(CROSS_AISLES[1]-1),
                  ITEMS_IN_BLOCK[2]*(CROSS_AISLES[2]-1))
PRODUCT_CAPACITY = (ITEMS_IN_AISLE[0] * STORAGE_COLUMNS[0] * HEIGHT[0],
                    ITEMS_IN_AISLE[1] * STORAGE_COLUMNS[1] * HEIGHT[1],
                    ITEMS_IN_AISLE[2] * STORAGE_COLUMNS[2] * HEIGHT[2])

# Storage policy specification - ABC
CLASS_A_CAPACITY = (125, 1250, 12500)
CLASS_B_CAPACITY = (125, 1250, 12500)
CLASS_C_CAPACITY = (250, 2500, 25000)

CLASS_A_UNIQUE = 0.25
CLASS_B_UNIQUE = 0.50
CLASS_C_UNIQUE = 0.90

random.seed(123)


# As a function for future extension.
def calculate_pick_speed(height: int):
    return PICK_SPEED[height]


# Calculates Manhattan distance + pick speed
def calculate_distance(wh_type: int, column: int, row: int, height: int):
    """ Returns time to pick item in seconds at given location.

    All indices start from 0.
    :param wh_type: 0,1,2 - small, med, large
    :param column: index of column in a warehouse
    :param row: index of row computed from top to bottom
    :param height: height level of item
    :return: time in sec
    """
    return ((column // 2) * AISLE_DIST + row * PICK_LOC_DIST) / WALK_SPEED + calculate_pick_speed(height)


def generate_items(warehouse_type: int):
    """ Generates items using the ABC storage method for further processing.

    :param warehouse_type:
    :return: Tuple: (a_class,b_class,c_class) items.
    """
    type_a_unique = int(np.round(CLASS_A_CAPACITY[warehouse_type] * CLASS_A_UNIQUE))
    type_b_unique = int(np.round(CLASS_B_CAPACITY[warehouse_type] * CLASS_B_UNIQUE))
    type_c_unique = int(np.round(CLASS_C_CAPACITY[warehouse_type] * CLASS_C_UNIQUE))

    a_class_unique = []
    b_class_unique = []
    c_class_unique = []
    a_class = []
    b_class = []
    c_class = []

    type_a_range = (0, type_a_unique)
    type_b_range = (type_a_range[1]+1, type_a_range[1] + type_b_unique)
    type_c_range = (type_b_range[1]+1, type_b_range[1] + type_c_unique)

    # Generate one of each item type
    for i in range(type_c_range[1]):
        if i < type_a_range[1]:
            a_class_unique.append(i)
            a_class.append(i)
        elif i < type_b_range[1]:
            b_class_unique.append(i)
            b_class.append(i)
        else:
            c_class_unique.append(i)
            c_class.append(i)

    # Generate the rest of the items - 60,30,10 distribution
    for i in range(type_a_unique, CLASS_A_CAPACITY[warehouse_type]):
        roll = random.random()
        if roll < 0.6:
            a_class.append(random.randint(type_a_range[0], type_a_range[1]))
        elif roll < 0.9:
            a_class.append(random.randint(type_b_range[0], type_b_range[1]))
        else:
            a_class.append(random.randint(type_c_range[0], type_c_range[1]))

    for i in range(type_b_unique, CLASS_B_CAPACITY[warehouse_type]):
        roll = random.random()
        if roll < 0.6:
            b_class.append(random.randint(type_a_range[0], type_a_range[1]))
        elif roll < 0.9:
            b_class.append(random.randint(type_b_range[0], type_b_range[1]))
        else:
            b_class.append(random.randint(type_c_range[0], type_c_range[1]))

    for i in range(type_c_unique, CLASS_C_CAPACITY[warehouse_type]):
        roll = random.random()
        if roll < 0.1:
            c_class.append(random.randint(type_a_range[0], type_a_range[1]))
        elif roll < 0.4:
            c_class.append(random.randint(type_b_range[0], type_b_range[1]))
        else:
            c_class.append(random.randint(type_c_range[0], type_c_range[1]))

    return random.sample(a_class, k=len(a_class)) + \
           random.sample(b_class, k=len(b_class)) + \
           random.sample(c_class, k=len(c_class))


# Assuming grid layout.
def assign_items_into_storage(wh_type):
    dimensions = (STORAGE_COLUMNS[wh_type], ITEMS_IN_AISLE[wh_type], HEIGHT[wh_type])
    shelves = np.zeros(dimensions)
    distances = np.zeros(dimensions)
    dist_list = []

    # Calculate and sort distances for division into zones.
    for column in range(STORAGE_COLUMNS[wh_type]):
        for row in range(ITEMS_IN_AISLE[wh_type]):
            for height in range(HEIGHT[wh_type]):
                dist = calculate_distance(0, column, row, height)
                distances[column, row, height] = dist
                dist_list.append(dist)
    dist_list.sort()
    ab_divider = dist_list[int(np.round(PRODUCT_CAPACITY[wh_type] * 0.75))]    # 25% of items, 75% of demand
    bc_divider = dist_list[int(np.round(PRODUCT_CAPACITY[wh_type] * 0.90))]    # 50% of items  90% of demand
    random_items = generate_items(wh_type)

    front_index = 0
    end_index = len(random_items) - 1
    left_out_indices = []

    # TODO: Fix - same item multiple times at the same location
    for column in range(STORAGE_COLUMNS[wh_type]):
        for row in range(ITEMS_IN_AISLE[wh_type]):
            for height in range(HEIGHT[wh_type]):
                if distances[column, row, height] <= ab_divider:
                    shelves[column, row, height] = random_items[front_index]
                    front_index += 1
                elif distances[column, row, height] > bc_divider:
                    shelves[column, row, height] = random_items[end_index]
                    end_index -= 1
                else:
                    left_out_indices.append((column, row, height))

    for column, row, height in left_out_indices:
        shelves[column, row, height] = random_items[front_index]
        front_index += 1

    verify_distribution(wh_type, shelves)

    return shelves


def verify_distribution(wh_type, shelves):
    distribution = np.zeros(PRODUCT_CAPACITY[wh_type])
    for column in range(STORAGE_COLUMNS[wh_type]):
        for row in range(ITEMS_IN_AISLE[wh_type]):
            for height in range(HEIGHT[wh_type]):
                distribution[int(shelves[column, row, height])] += 1
    print()


# For each item, saves all locations of the item.
def find_items(graph, max_items):
    positions = [None] * max_items
    for i in range(len(positions)):
        positions[i] = []

    for idx, vertex in enumerate(graph.nodes):
        vertex = graph.nodes[vertex]
        if vertex['type'] == "Steiner node" or vertex['type'] == "Depot":
            continue
        for item1, item2 in zip(vertex['left'], vertex['right']):
            positions[int(item1)].append(idx)
            positions[int(item2)].append(idx)
    return positions


# Generates items, assigns them into storage, and generates full networkx graph according to wh_type specifications.
def generate_warehouse_graph(wh_type):
    """ Generates full Networkx graph.

    Generates items, assigns the items into storage locations and generates graph according to wh_type specifications.
    The graph includes cross aisles and depot.

    :param wh_type: The warehouse type.
    :return: Networkx graph.
    """
    items = assign_items_into_storage(wh_type)
    graph = nx.Graph()

    graph.add_node("0")
    graph.nodes["0"]['type'] = "Depot"

    for cross_aisle in range(CROSS_AISLES[wh_type]):
        for aisle in range(AISLES[wh_type]):
            # Add highway node into graph
            node_index = "x" + str(aisle) + "y" + str(cross_aisle*ITEMS_IN_BLOCK[wh_type] + cross_aisle)
            graph.add_node(node_index)
            graph.nodes[node_index]["type"] = "Steiner node"
            graph.nodes[node_index]["x"] = aisle
            graph.nodes[node_index]["y"] = cross_aisle*ITEMS_IN_BLOCK[wh_type]
            graph.nodes[node_index]["left"] = None
            graph.nodes[node_index]["right"] = None

            # Add edge connecting the vertex with the adjacent vertex to the left
            if aisle != 0:
                prev_index = "x" + str(aisle-1) + "y" + str(cross_aisle*ITEMS_IN_BLOCK[wh_type] + cross_aisle)
                graph.add_edge(prev_index, node_index, weight="240")

    # Add all item vertices
    for aisle in range(AISLES[wh_type]):
        for position in range(ITEMS_IN_AISLE[wh_type]):
            node_index = "x" + str(aisle) + "y" + str(position + (position // ITEMS_IN_BLOCK[wh_type]) + 1)
            prev_index = "x" + str(aisle) + "y" + str(position + (position // ITEMS_IN_BLOCK[wh_type]))
            graph.add_node(node_index)
            graph.nodes[node_index]["type"] = "Shelf node"
            graph.nodes[node_index]["x"] = aisle
            graph.nodes[node_index]["y"] = position + (position // ITEMS_IN_BLOCK[wh_type]) + 1
            graph.nodes[node_index]["left"] = items[2 * aisle, position]
            graph.nodes[node_index]["right"] = items[2 * aisle + 1, position]
            graph.add_edge(node_index, prev_index, weight="120")
            if (position + 1) % ITEMS_IN_BLOCK[wh_type] == 0:
                next_index = "x" + str(aisle) + "y" + str(position + (position // ITEMS_IN_BLOCK[wh_type]) + 2)
                graph.add_edge(next_index, node_index, weight="120")
    graph.add_edge("0", "x0y0", weight="0")

    return graph


# Generates random set of orders
def generate_order(wh_graph):
    item_positions = find_items(wh_graph, 50000)
    items = []

    max_item_idx = np.max(np.where(item_positions))
    order_size = random.randint(6, 12)

    for i in range(order_size):
        item_idx = random.randint(0, max_item_idx)
        items.append(item_positions[item_idx])

    return items


def generate_and_serialize_instance(wh_type, orders_count, file_path):
    graph = generate_warehouse_graph(wh_type)
    vertices = [vertex for vertex in graph.nodes]
    orders = []

    for i in range(orders_count):
        orders.append(generate_order(graph))

    file = io.open(file_path, "w+")
    file.write("Test warehouse instance of type " + str(wh_type) + "\n")
    file.write("Vertices: " + str(len(vertices)) + "\n")

    for i, vertex in enumerate(vertices):
        file.write(str(i) + " " + vertex + " " + graph.nodes[vertex]["type"] + "\n")
        if graph.nodes[vertex]["type"] == "Shelf node":
            file.write(str(graph.nodes[vertex]["left"].astype(int)) + "\n")
            file.write(str(graph.nodes[vertex]["right"].astype(int)) + "\n")

    file.write("Edges:\n")

    for edge in graph.edges:
        file.write(str(edge) + " ")

    file.write("Orders:\n")
    for i in range(orders_count):
        file.write("Order number " + str(i) + ", items: " + str(len(orders[i])) + "\n")
        for items in orders[i]:
            file.write(str(items) + "\n")

    file.close()
    print()


generate_and_serialize_instance(0, 5, "../data/whole_instance.txt")
