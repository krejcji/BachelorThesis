import io
import numpy as np

import test_warehouse_generator as twg


def generate_glns_instance(wh_type, products):
    """ Generates an generalized TSP instance for GLNS solver.

    :param wh_type:
    :param products:
    :return:
    """
    graph = twg.generate_warehouse_graph(wh_type)
    orig_vertices = [vertex for vertex in graph.nodes]
    special_vertices = []
    item_positions = twg.find_items(graph, 50000)
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
    time_steps = int(twg.PRODUCT_CAPACITY[wh_type] // twg.HEIGHT[wh_type] // 2) + 10
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
