def INFERTYPES(CallGraph, T):
    B = {}  # B is a map from type variable to sketch.
    for S in REVERSEPOSTORDER(CallGraph.sccs):
        C = {}
        for P in S:
            T[P] = {}
        for P in S:
            C_d = T[P]
            SOLVE(C_d, B)
            REFINEPARAMETERS(P, B)
            C = CONSTRAINTS(P, T)
            SOLVE(C, B)
    A = {}
    for x in B:
        A[x] = SKETCHTOAPPXCTYPE(B[x])
    return A

def SOLVE(C, B):
    C = INFERSHAPES(C, B)
    Q = TRANSDUCER(C,Λ)
    for λ in Λ:
        for Xu where λ Q:> Xu:
            νB[X](u) = νB[X](u) ∨ λ
        for all Xu such that Xu Q:> λ:
            νB[X](u) ← νB[X](u) ∧ λ

def REFINEPARAMETERS(P, B):
    for i in P.formalIns:
        λ = TOP
        for a in P.actualIns(i):
            λ = λ u B[a]
        B[i] = B[i] u λ

    for o in P.formalOuts:
        λ = BOTTOM
        for a in P.actualOuts(o):
            λ = λ u B[a]
        B[o] = B[o] t λ

def INFERPROCTYPES(CallGraph):
    T = {} # T is a map from procedure to type scheme.
    for S in POSTORDER(CallGraph.sccs):
        C = {}
        for P in S:
            T[P] = {}
        for P in S:
            C = C ∪ CONSTRAINTS(P, T)
        
        C = INFERSHAPES(C,{})
        for P in S:
            V = P.formalIns ∪ P.formalOuts
            Q = TRANSDUCER(C, V ∪ Λ)
            T[P] = TYPESCHEME(Q)

def CONSTRAINTS(P : Proc, T : TypeScheme):
    C = {}
    for i in P.instructions:
        C = C ∪ ABSTRACTINTERP(TypeInterp, i)
        if i calls Q:
            C = C ∪ INSTANTIATE(T[Q], i)
    return C

# Convert transducer to PD system
def TYPESCHEME(Q):
    ∆  = new PDS
    ∆.states = Q.states
    for (p, q) in Q.transitions:
        if t = pop ` then
            ADDPDSRULE(∆,h, (p, l), (q, epsilon))
        else:
            ADDPDSRULE(∆,h (p,epsilon), (q,l))
    return ∆

    