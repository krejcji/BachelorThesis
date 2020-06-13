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

    # Generate the rest of the items - 75,15,10 distribution
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
        if roll < 0.9:
            b_class.append(random.randint(type_a_range[0], type_a_range[1]))
        elif roll < 0.6:
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


def generate_warehouse_items(wh_type):
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


def generate_gtsp_instance(wh_type, products):
    graph = generate_warehouse_graph(wh_type)
    orig_vertices = [vertex for vertex in graph.nodes]
    special_vertices = []
    item_positions = find_items(graph, 50000)
    products_positions = []
    for product_idx, product in enumerate(products):
        positions = item_positions[product]
        products_positions.append(positions)
        special_vertices.append([])
        # Create special vertices
        for idx_2, vertex_idx in enumerate(positions):
            node_index = "special_" + str(product_idx) + "_" + str(idx_2)
            graph.add_node(node_index)
            graph.nodes[node_index]["type"] = "Special"
            graph.nodes[node_index]["x"] = vertex_idx
            graph.nodes[node_index]["y"] = product_idx
            graph.nodes[node_index]["left"] = None
            graph.nodes[node_index]["right"] = None
            special_vertices[product_idx].append(node_index)

    enum = [vertex for vertex in graph.nodes]
    vertex_dict = {}
    for idx, vertex in enumerate(enum):
        vertex_dict[vertex] = idx
    time_steps = int(PRODUCT_CAPACITY[wh_type] // HEIGHT[wh_type] // 2) + 10
    layer_size = len(enum) - 1
    final_size = (time_steps-1) * layer_size + time_steps
    gtsp_distances = np.full((final_size, final_size), time_steps * 1000)

    min_dist = 2
    for time in range(1, min_dist):
        gtsp_distances[time, time+1] = 0

    # Set distances for layer substitution vertices
    for time in range(min_dist, time_steps):
        for time_1 in range(min_dist, time_steps):
            if time <= time_1:
                gtsp_distances[time, time_1] = 0

    gtsp_distances[0, time_steps] = 1

    '''
    for time in range(1, time_steps):
        for time_2 in range(1, time_steps):
            gtsp_distances[time, time_2] = 50000
    '''
    # Fill in edges from layer time into time+1
    for time in range(0, time_steps-1):
        for idx, vertex_str in enumerate(enum):
            vertex_ref = graph.nodes[vertex_str]
            if vertex_ref["type"] == "Depot":
                continue
            if vertex_ref["type"] == "Steiner node" or vertex_ref["type"] == "Shelf node":
                if vertex_str == "x0y0":
                    # Set return path through substitution vertices.
                    gtsp_distances[time_steps + (time*layer_size) + (idx-1), time+2] = 1
                for neighbor in graph.neighbors(vertex_str):
                    neighbor_idx = vertex_dict[neighbor]
                    if neighbor_idx == 0:
                        continue
                    if time < time_steps - 2:
                        gtsp_distances[time_steps + (time*layer_size) + (idx-1),
                                       time_steps + ((time+1)*layer_size) + (neighbor_idx-1)] = 1
            if vertex_ref["type"] == "Special":
                regular_idx = vertex_ref["x"]
                if time > 0:
                    # Regular vertex in t-1 to the special pick vertex edge
                    gtsp_distances[time_steps + ((time-1) * layer_size) + (regular_idx - 1),
                                   time_steps + (time * layer_size) + (idx - 1)] = 1
                    # print("Written enge from " + str(time_steps + ((time-1) * layer_size) + (regular_idx - 1)) + "to " + str(time_steps + (time * layer_size) + (idx - 1)))
                    if time < time_steps - 4:
                        # This pick vertex edge into regular again in t+15
                        gtsp_distances[time_steps + (time * layer_size) + (idx - 1),
                                       time_steps + ((time+3) * layer_size) + (regular_idx - 1)] = 3

    out = io.open("output", 'w+')
    out.write("NAME: 65rbg323" + "\n")
    out.write("TYPE: AGTSP" + "\n")
    out.write("COMMENT: Stacker crane application (Ascheuer)" + "\n")
    out.write("DIMENSION: " + str(final_size) + "\n")
    out.write("GTSP_SETS: " + str(time_steps-1 + len(products) + 1) + "\n")
    out.write("EDGE_WEIGHT_TYPE: EXPLICIT" + "\n")
    out.write("EDGE_WEIGHT_FORMAT: FULL_MATRIX " + "\n")
    out.write("EDGE_WEIGHT_SECTION" + "\n")
    for x in range(final_size):
        line = ""
        for y in range(final_size):
            line += " " + str(gtsp_distances[x, y])
        line += "\n"
        out.write(line)

    out.write("GTSP_SET_SECTION:" + "\n")

    # Add depot.
    out.write("1 1 -1\n")
    set_idx = 2

    # Add set for each graph time step.
    for i in range(1, time_steps):
        out.write(str(set_idx) + " ")
        out.write(str(i+1) + " ")
        for j in range(1, len(orig_vertices)):
            out.write(str(time_steps + ((i-1) * layer_size) + j) + " ")
        out.write("-1\n")
        set_idx += 1

    index_offset = len(orig_vertices)
    # Add set for each items picking locations.
    for i in range(len(special_vertices)):
        out.write(str(set_idx) + " ")
        set_idx += 1
        for vertex in special_vertices[i]:
            for time in range(1, time_steps):
                out.write(str(time_steps + ((time-1) * layer_size) + index_offset) + " ")
            index_offset += 1
        out.write("-1\n")

    out.write("EOF" + "\n")
    out.close()
    print()


def get_reverse_index(i):
    i = i - 51
    time = i // 75
    index = i % 75
    return time, index


def generate_warehouse_graph(wh_type):
    items = generate_warehouse_items(wh_type)
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


def generate_dag(wh_type, pick_locations, classes, time_steps):
    graph = generate_warehouse_graph(wh_type)
    vertices = [vertex for vertex in graph.nodes]
    picking_vertices = random.sample(range(1, len(vertices)), pick_locations)
    vertices_by_class_str = [None] * classes
    vertices_by_class_idx = [None] * classes

    # Generate random sample of picking locations and division into classes
    for i in range(classes):
        vertices_by_class_str[i] = []
        vertices_by_class_idx[i] = []
    step = 1 / classes
    for vertex in picking_vertices:
        rnd = random.random()
        divider = step
        i = 0
        while True:
            if rnd < divider:
                vertices_by_class_str[i].append(vertices[vertex])
                vertices_by_class_idx[i].append(vertex)
                break
            else:
                divider += step
                i += 1

    pick_locations_list = ["x0y0"]
    for clas in vertices_by_class_str:
        for vertex in clas:
            pick_locations_list.append(vertex)

    shortest_distances = np.zeros((len(pick_locations_list), len(pick_locations_list)))
    # Prepare the distance matrix
    for i, vertex in enumerate(pick_locations_list):
        for j, vertex_2 in enumerate(pick_locations_list):
            shortest_distances[i, j] = nx.algorithms.shortest_path_length(graph, vertex, vertex_2)

    distances = np.zeros((pick_locations+1, time_steps))
    non_zero_ctr = 1
    # Fill in the initial distance
    distances[0, 0] = 1

    dist_sets = [None] * (pick_locations+1)
    for s in range(len(dist_sets)):
        dist_sets[s] = [None] * time_steps
        for ss in range(len(dist_sets[s])):
            dist_sets[s][ss] = []

    class_list = []
    j = 1
    class_list.append(0)
    for clas in range(len(vertices_by_class_idx)):
        for vertex in vertices_by_class_idx[clas]:
            class_list.append(j)
        j += 1

    # By the use of dynamic programming, fill in the distance graph.
    for time in range(130):
        for vertex in range(len(pick_locations_list)):
            if distances[vertex, time] != 0:
                for vertex_target in range(len(pick_locations_list)):
                    if vertex != vertex_target:
                        vertex_class = class_list[vertex]
                        vertex_target_class = class_list[vertex_target]
                        if vertex_class == vertex_target_class:
                            continue
                        distance = int(shortest_distances[vertex, vertex_target]) + 20
                        #random.randint(1, 15) + \
                        if time+distance < time_steps:
                            distances[vertex_target, time+distance] = 1
                            non_zero_ctr += 1
                            if vertex != 0:
                                if len(dist_sets[vertex][time]):
                                    for sett in dist_sets[vertex][time]:
                                        new_set = set()
                                        for item in sett:
                                            new_set.add(item)
                                        new_set.add(vertex_class)
                                        dist_sets[vertex_target][time+distance].append(new_set)
                                else:
                                    new_set = set()
                                    new_set.add(vertex_class)
                                    dist_sets[vertex_target][time + distance].append(new_set)

    print()
# 14 sec with distances search

generate_dag(1, 50, 10, 1000)
# generate_gtsp_instance(0, [1, 10, 100, 250, 300])
