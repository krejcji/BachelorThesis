import sys
import os
import io
import os.path
import random

import numpy as np
import networkx as nx
import matplotlib.pyplot as plt

# General parameters
WALK_SPEED = 100         # cm/sec
# 1 edge per time unit
PICK_SPEED = (30, 60, 90, 120, 120)      # sec - default values
AISLE_DIST = 360         # cm
PICK_LOC_DIST = 120      # cm

# Warehouse specification - (type 0, type 1, type 3)
AISLES = (10, 20, 40)
STORAGE_COLUMNS = (AISLES[0] * 2, AISLES[1] * 2, AISLES[2] * 2)
ITEMS_IN_BLOCK = (30, 30, 30)
CROSS_AISLES = (3, 5, 6)
HEIGHT = (5, 5, 5)
ITEMS_IN_AISLE = (ITEMS_IN_BLOCK[0]*(CROSS_AISLES[0]-1),
                  ITEMS_IN_BLOCK[1]*(CROSS_AISLES[1]-1),
                  ITEMS_IN_BLOCK[2]*(CROSS_AISLES[2]-1))
PRODUCT_CAPACITY = (ITEMS_IN_AISLE[0] * STORAGE_COLUMNS[0] * HEIGHT[0],
                    ITEMS_IN_AISLE[1] * STORAGE_COLUMNS[1] * HEIGHT[1],
                    ITEMS_IN_AISLE[2] * STORAGE_COLUMNS[2] * HEIGHT[2])
DEPOT_NODES = (10, 20, 40)

# Storage policy specification - ABC  --not used currently
CLASS_A_CAPACITY = (1000, 4000, 10000)
CLASS_B_CAPACITY = (2000, 8000, 20000)
CLASS_C_CAPACITY = (3000, 12000, 30000)

CLASS_A_UNIQUE = 0.25
CLASS_B_UNIQUE = 0.50
CLASS_C_UNIQUE = 0.90

UNIQUE_RANDOM = 0.075

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


def generate_and_assign_items_random(wh_type):
    wh_capacity = PRODUCT_CAPACITY[wh_type]
    unique_items = int(UNIQUE_RANDOM * wh_capacity)
    items = np.zeros((STORAGE_COLUMNS[wh_type], ITEMS_IN_AISLE[wh_type], HEIGHT[wh_type], 2))

    # Generate each unique item into some position
    for i in range(1, unique_items+1):
        while True:
            j = random.randint(0, STORAGE_COLUMNS[wh_type]-1)
            k = random.randint(0, ITEMS_IN_AISLE[wh_type]-1)
            l = random.randint(0, HEIGHT[wh_type]-1)
            if items[j, k, l, 0] == 0:
                items[j, k, l, 0] = i
                items[j, k, l, 1] = PICK_SPEED[l]
                break

    # Fill in the rest of free positions with random items
    for i in range(STORAGE_COLUMNS[wh_type]):
        for j in range(ITEMS_IN_AISLE[wh_type]):
            used = set()
            for k in range(HEIGHT[wh_type]):
                if items[i, j, k, 0] != 0:
                    used.add(items[i, j, k, 0])
            for k in range(HEIGHT[wh_type]):
                if items[i, j, k, 0] != 0:
                    continue
                while True:
                    item = random.randint(1, unique_items)
                    if item not in used:
                        used.add(item)
                        items[i, j, k, 0] = item
                        items[i, j, k, 1] = PICK_SPEED[k]
                        break
    return items


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
        for height, (item1, item2) in enumerate(zip(vertex['left'], vertex['right'])):
            positions[int(item1[0])].append((idx, 0, height))
            positions[int(item2[0])].append((idx, 1, height))
    return positions


# Generates items, assigns them into storage, and generates full networkx graph according to wh_type specifications.
def generate_warehouse_graph(wh_type):
    """ Generates full Networkx graph.

    Generates items, assigns the items into storage locations and generates graph according to wh_type specifications.
    The graph includes cross aisles and depot.

    :param wh_type: The warehouse type.
    :return: Networkx graph.
    """
    items = generate_and_assign_items_random(wh_type)
    graph = nx.Graph()

    depot_per_side = DEPOT_NODES[wh_type] // 2

    graph.add_node("0")
    graph.nodes["0"]['type'] = "Depot"

    for cross_aisle in range(CROSS_AISLES[wh_type]):
        for aisle in range(((AISLES[wh_type]-1)*3)+1):
            # Add 2 highway node into graph
            node_index_x = aisle
            node_index_y = cross_aisle*ITEMS_IN_BLOCK[wh_type] + (2 * cross_aisle)
            node_index_0 = "x" + str(node_index_x) + "y" + str(node_index_y)
            graph.add_node(node_index_0)
            graph.nodes[node_index_0]["type"] = "Steiner node"
            graph.nodes[node_index_0]["x"] = node_index_x
            graph.nodes[node_index_0]["y"] = node_index_y
            graph.nodes[node_index_0]["left"] = None
            graph.nodes[node_index_0]["right"] = None

            node_index_y += 1
            node_index_1 = "x" + str(node_index_x) + "y" + str(node_index_y)
            graph.add_node(node_index_1)
            graph.nodes[node_index_1]["type"] = "Steiner node"
            graph.nodes[node_index_1]["x"] = node_index_x
            graph.nodes[node_index_1]["y"] = node_index_y
            graph.nodes[node_index_1]["left"] = None
            graph.nodes[node_index_1]["right"] = None

            # Add vertical edge
            graph.add_edge(node_index_0, node_index_1, weight="120")

            # Add edge connecting the vertex with the adjacent vertex to the left
            if aisle != 0:
                prev_index = "x" + str(aisle-1) + "y" + str(cross_aisle*ITEMS_IN_BLOCK[wh_type] + (2*cross_aisle))
                graph.add_edge(prev_index, node_index_0, weight="240")
                prev_index_1 = "x" + str(aisle - 1) + "y" + str(cross_aisle * ITEMS_IN_BLOCK[wh_type] + (2 * cross_aisle) + 1)
                graph.add_edge(prev_index_1, node_index_1, weight="240")

    # Add all item vertices
    for aisle in range(AISLES[wh_type]):
        x_pos = aisle*3
        for position in range(ITEMS_IN_AISLE[wh_type]):
            node_index = "x" + str(x_pos) + "y" + str(position + ((position // ITEMS_IN_BLOCK[wh_type])*2) + 2)
            prev_index = "x" + str(x_pos) + "y" + str(position + ((position // ITEMS_IN_BLOCK[wh_type])*2) + 1)
            graph.add_node(node_index)
            graph.nodes[node_index]["type"] = "Shelf node"
            graph.nodes[node_index]["x"] = x_pos
            graph.nodes[node_index]["y"] = x_pos + ((position // ITEMS_IN_BLOCK[wh_type])*2) + 2
            graph.nodes[node_index]["left"] = items[2 * aisle, position]
            graph.nodes[node_index]["right"] = items[2 * aisle + 1, position]
            graph.add_edge(node_index, prev_index, weight="120")
            if (position + 1) % ITEMS_IN_BLOCK[wh_type] == 0:
                next_index = "x" + str(x_pos) + "y" + str(position + (position // ITEMS_IN_BLOCK[wh_type]*2) + 3)
                graph.add_edge(next_index, node_index, weight="120")
    graph.add_edge("0", "x0y0", weight="0")

    return graph


# Generates random set of orders
def generate_order(wh_graph):
    item_positions = find_items(wh_graph, 50000)
    items = []

    max_item_idx = np.max(np.where(item_positions))
    order_size = random.randint(6, 12)

    used = set()
    for i in range(order_size):
        while True:
            item_idx = random.randint(0, max_item_idx)
            if item_idx not in used:
                used.add(item_idx)
                items.append(item_positions[item_idx])
                break

    return items


def generate_and_serialize_instance(wh_type, agents, orders_per_agent, file_path):
    graph = generate_warehouse_graph(wh_type)
    vertices = [vertex for vertex in graph.nodes]
    agents_orders = []

    for i in range(agents):
        agents_orders.append([])
        for j in range(orders_per_agent):
            agents_orders[i].append(generate_order(graph))

    file = io.open(file_path, "w+")
    file.write("Test warehouse instance of type " + str(wh_type) + "\n")
    file.write("Vertices: " + str(len(vertices)) + "\n")
    file.write("Items per vertex: " + str(HEIGHT[wh_type]) + "\n")

    for i, vertex in enumerate(vertices):
        file.write(str(i) + " " + vertex + " " + graph.nodes[vertex]["type"] + "\n")
        if graph.nodes[vertex]["type"] == "Shelf node":
            left = graph.nodes[vertex]["left"]
            right = graph.nodes[vertex]["right"]
            for j in range(HEIGHT[wh_type]):
                file.write(str(left[j][0].astype(int)) + "," + str(left[j][1].astype(int)) + " ")
            file.write("\n")
            for j in range(HEIGHT[wh_type]):
                file.write(str(right[j][0].astype(int)) + "," + str(right[j][1].astype(int)) + " ")
            file.write("\n")

    file.write("Edges:\n")

    for edge in graph.edges:
        file.write(edge[0] + "," + edge[1] + " ")
    file.write("\n")

    file.write("Agents: " + str(agents) + "\n")
    for i in range(agents):
        file.write("Agent: " + str(i) + ", Orders: " + str(len(agents_orders[i])) + "\n")
        for j in range(len(agents_orders[i])):
            file.write("Order: " + str(j) + ", classes: " + str(len(agents_orders[i][j])) + " Source " + str(2*i) +
                       " Target " + str(2*i) + "\n")
            for item_class in agents_orders[i][j]:
                for item in item_class:
                    file.write(str(item[0]) + "," + str(item[1]) + "," + str(item[2]) + " ")
                file.write("\n")

    file.close()
    print()


generate_and_serialize_instance(1, 10, 1, "../data/whole_instance.txt")
