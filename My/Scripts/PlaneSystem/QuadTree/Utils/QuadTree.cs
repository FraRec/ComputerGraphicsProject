using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quad {
    public Box2 Bounds          { get;      }
    public List<Quad> Children  { get; set; }
    public Vector2 Center       { get;      }
    public float Size           { get;      }

    public Quad(Box2 bounds, List<Quad> children, Vector2 center, float size) {
        this.Bounds = bounds;
        this.Children = children;
        this.Center = center;
        this.Size = size;
    }
}

public struct Box2 {
    public Vector2 MIN { get; }
    public Vector2 MAX { get; }

    public Box2(Vector2 min, Vector2 max) {
        this.MIN = min;
        this.MAX = max;
    }

    public Vector2 getCenter() {
        return MIN + (MAX - MIN) / 2f;
    }

    public float getSize() {
        return Vector2.Distance(MAX, MIN);
    }
}

public class QuadTree {

    private Quad _root;
    private float _MIN_NODE_SIZE;

    public QuadTree(Vector2 min, Vector2 max, float min_node_size) {
        this._MIN_NODE_SIZE = min_node_size;

        Box2 b = new Box2(min, max);
        this._root = new Quad(b, null, b.getCenter(), b.getSize());
    }

    public List<Quad> GetChildren() {
        List<Quad> children = new List<Quad>();
        this._GetChildren(this._root, children);
        return children;
    }

    private void _GetChildren(Quad node, List<Quad> target) {
        if (node.Children == null) {
            target.Add(node);
            return;
        }

        foreach(Quad c in node.Children) {
            this._GetChildren(c, target);
        }
    }

    public void Insert(Vector3 posToFollow, float yValue) {
        this._Insert(this._root, posToFollow, yValue);
    }

    private void _Insert(Quad node, Vector3 posToFollow, float yValue) {
        float dstToNode = this._DistanceToNode(node, posToFollow, yValue);

        if(dstToNode < node.Size && node.Size > _MIN_NODE_SIZE) {
            node.Children = this._CreateChildren(node);
            foreach (Quad c in node.Children) {
                this._Insert(c, posToFollow, yValue);
            }
        }
    }

    private float _DistanceToNode(Quad node, Vector3 pos, float yValue) {
        return Vector3.Distance(new Vector3(node.Center.x, yValue, node.Center.y), pos);
    }

    private List<Quad> _CreateChildren(Quad child) {
        Vector2 midpoint = child.Bounds.getCenter();

        // Bottom Left
        Box2 b1 = new Box2(
            child.Bounds.MIN,
            midpoint
        );

        // Bottom Right
        Box2 b2 = new Box2(
            new Vector2(midpoint.x, child.Bounds.MIN.y),
            new Vector2(child.Bounds.MAX.x, midpoint.y)
        );

        // Top Left
        Box2 b3 = new Box2(
            new Vector2(child.Bounds.MIN.x, midpoint.y),
            new Vector2(midpoint.x, child.Bounds.MAX.y)
        );

        // Rop Right
        Box2 b4 = new Box2(
            midpoint,
            child.Bounds.MAX
        );

        Quad quad1 = new Quad(b1, null, b1.getCenter(), b1.getSize());
        Quad quad2 = new Quad(b2, null, b2.getCenter(), b2.getSize());
        Quad quad3 = new Quad(b3, null, b3.getCenter(), b3.getSize());
        Quad quad4 = new Quad(b4, null, b4.getCenter(), b4.getSize());
        List<Quad> children = new List<Quad>() { quad1, quad2, quad3, quad4 };

        return children;
    }
}
