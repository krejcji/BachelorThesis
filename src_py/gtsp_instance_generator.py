import io
import random

import networkx as nx

import test_warehouse_generator as twg


def generate_gtsp_random_instance(wh_type, pick_locations, classes, file_path):
    """ Generates a random GTSP instance for testing purposes.

    :param wh_type:
    :param pick_locations:
    :param classes:
    :param file_path:
    :return:
    """
    graph = twg.generate_warehouse_graph(wh_type)
    vertices = [vertex for vertex in graph.nodes]

    # Generate random sample of vertices
    picking_vertices = random.sample(range(1, len(vertices)), pick_locations)
    picking_vertices_classes = []

    # Generate random picking locations and division into classes
    step = 1 / classes
    for vertex in picking_vertices:
        rnd = random.random()
        divider = step
        i = 1
        while True:
            if rnd < divider:
                picking_vertices_classes.append(i)
                break
            else:
                divider += step
                i += 1

    file = io.open(file_path, "w+")
    file.write(str(len(vertices)) + "\n")
    file.write(str(len(picking_vertices)+1) + "\n")
    file.write(str(classes+1) + "\n")

    for i, vertex in enumerate(vertices):
        line = ""
        for j, vertex_2 in enumerate(vertices):
            line += str(nx.algorithms.shortest_path_length(graph, vertex, vertex_2)) + " "
        file.write(line + "\n")
    file.write("0 0\n")

    for i in range(len(picking_vertices)):
        file.write(str(picking_vertices[i]) + " " + str(picking_vertices_classes[i]) + "\n")
    file.close()
