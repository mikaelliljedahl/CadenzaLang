# Cadenza: Philosophy and FAQ

This document clarifies the core philosophy behind Cadenza and addresses common, valid questions from experienced developers. It aims to explain why Cadenza is designed the way it is and what future it is optimizing for.

## The Core Thesis: Optimizing for Correctness, Not Just Generation

The central premise of Cadenza is not that Large Language Models (LLMs) cannot write code in existing languages like C# or Python. They can, and they are becoming remarkably proficient at it.

The problem is that existing languages were designed for human developers. They offer flexibility and multiple ways to solve a problem, trusting the developer's experience to navigate complexities like concurrency, side effects, and error handling. This flexibility is also a source of ambiguity, leading to code that is often syntactically correct but semantically fragile, containing subtle bugs, race conditions, or unhandled edge cases.

Cadenza's thesis is this: In a future where code is primarily written and maintained by AI, the most important attribute of a language is not its flexibility, but its unambiguous clarity, verifiability, and inherent safety.

It is a language designed to increase the probability that generated code is robust, predictable, and correct from 80% to 99%, by eliminating the ambiguities that both LLMs and humans struggle with.

## Frequently Asked Questions (FAQ)

### Q: Isn't this a "solution looking for a problem"? LLMs are already good at C# and Python.

This is the most common and important question. While an LLM can generate a C# function, it struggles with the implicit context. It might forget to use ConfigureAwait(false), introduce a race condition with a static variable, or miss a null check in a complex object graph.

Cadenza solves this by making context explicit.

- Effects (uses [Database]) are part of the function's contract.
- Error handling (Result<T, E>) is mandatory and checked by the compiler.
- Immutability is the default, preventing entire classes of side-effect bugs.

The goal isn't to make generation possible; it's to make correct generation the path of least resistance.

### Q: Why add the overhead of a new language and compiler instead of just using linters and good coding standards?

Linters and standards are helpful "guardrails," but they are optional and can be bypassed or misconfigured. Cadenza treats these concepts as "concrete walls" built into the language itself.

- A linter might warn you about a function with hidden side effects.
- The Cadenza compiler will fail to compile it.

This shifts the burden of ensuring correctness from developer discipline and tooling configuration to the fundamental, non-negotiable rules of the language.

### Q: How are spec blocks better than just writing good comments?

The key difference is that /*spec ... */ blocks are machine-readable structured data, not just text.

A regular comment is passive. A spec block is an active part of the development workflow. It enables a CI/CD pipeline step where another LLM can be asked: "Does the following code correctly and completely implement all rules in its accompanying spec block?"

This transforms the specification from simple documentation into a testable, verifiable contract between the intent and the implementation.

### Q: Doesn't "one way to do things" limit the flexibility needed for real-world problems?

Cadenza is intentionally "opinionated" for the most common application development tasks (APIs, data processing, UI). It believes that for 95% of these tasks, there are established best practices that should be enforced for safety and clarity.

For the other 5% of tasks that require low-level performance tuning or complex algorithms, Cadenza is not intended to be the best tool. The design should include a clean Foreign Function Interface (FFI) to call out to raw C# (or another target language) for those specific, high-performance needs. It provides safety for the common case and an escape hatch for the exceptional case.

### Q: Why start with a C#-only target? Isn't that too limiting?

The choice of C# and .NET is a pragmatic starting point. It provides a world-class, high-performance runtime, a mature ecosystem, and excellent tools for compilation (Roslyn).

However, the principles of Cadenza are platform-agnostic. The long-term vision is to have a suite of compilers that can target different environments using the same core language:

- Backend: C# / .NET
- Frontend: JavaScript / WebAssembly
- Mobile: Swift / Kotlin or native binaries

This would fulfill the ultimate promise of a single, safe, and unified language for building entire full-stack applications.
