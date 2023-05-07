﻿using System.Runtime.InteropServices;

namespace LlamaCppLib
{
    using llama_context = System.IntPtr;
    using llama_token = System.Int32;

    public static class LlamaCppInterop
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct llama_token_data
        {
            public llama_token id;
            public float logit;
            public float p;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct _llama_token_data_array
        {
            public nint data;
            public long size;
            [MarshalAs(UnmanagedType.I1)]
            public bool sorted;
        }

        public struct llama_token_data_array
        {
            public List<llama_token_data> data;
            public bool sorted;
        }

        public delegate void llama_progress_callback(float progress, llama_context ctx);

        [StructLayout(LayoutKind.Sequential)]
        public struct llama_context_params
        {
            public int n_ctx;
            public int n_parts;
            public int seed;
            [MarshalAs(UnmanagedType.I1)]
            public bool f16_kv;
            [MarshalAs(UnmanagedType.I1)]
            public bool logits_all;
            [MarshalAs(UnmanagedType.I1)]
            public bool vocab_only;
            [MarshalAs(UnmanagedType.I1)]
            public bool use_mmap;
            [MarshalAs(UnmanagedType.I1)]
            public bool use_mlock;
            [MarshalAs(UnmanagedType.I1)]
            public bool embedding;
            public llama_progress_callback progress_callback;
            public nint progress_callback_user_data;
        }

        public enum LLAMA_FTYPE
        {
            ALL_F32 = 0,
            MOSTLY_F16 = 1,  // except 1d tensors
            MOSTLY_Q4_0 = 2,  // except 1d tensors
            MOSTLY_Q4_1 = 3,  // except 1d tensors
            MOSTLY_Q4_1_SOME_F16 = 4, // tok_embeddings.weight and output.weight are F16
            MOSTLY_Q4_2 = 5,  // except 1d tensors
            // MOSTLY_Q4_3 (6) support has been removed
            MOSTLY_Q8_0 = 7,  // except 1d tensors
            MOSTLY_Q5_0 = 8,  // except 1d tensors
            MOSTLY_Q5_1 = 9,  // except 1d tensors
        }

        [DllImport("llama")]
        public static extern llama_context_params llama_context_default_params();

        [DllImport("llama")]
        public static extern bool llama_mmap_supported();

        [DllImport("llama")]
        public static extern bool llama_mlock_supported();

        /// <summary>
        /// Various functions for loading a ggml llama model.
        /// Allocate (almost) all memory needed for the model.
        /// </summary>
        /// <param name="path_model">Model file path</param>
        /// <param name="cparams">Parameters to use for loading the model</param>
        /// <returns>LlamaContext on success or null on failure</returns>
        [DllImport("llama")]
        public static extern llama_context llama_init_from_file(string path_model, llama_context_params cparams);

        /// <summary>
        /// Frees all allocated memory
        /// </summary>
        /// <param name="ctx">LlamaContext</param>
        [DllImport("llama")]
        public static extern void llama_free(llama_context ctx);

        /// <summary>
        /// TODO: not great API - very likely to change (from llama.cpp)
        /// </summary>
        /// <param name="ctx">LlamaContext</param>
        /// <param name="path_lora">Lora adapter file path</param>
        /// <param name="path_base_model">Model file path</param>
        /// <param name="n_threads">nthread - how many threads to use. If <=0, will use std::thread::hardware_concurrency(), else the number given</param>
        /// <returns>Returns 0 on success</returns>
        [DllImport("llama", EntryPoint = "llama_model_quantize")]
        public static extern int llama_model_quantize(string fname_inp, string fname_out, LLAMA_FTYPE ftype, int nthread);

        /// <summary>
        /// Apply a LoRA adapter to a loaded model
        /// The model needs to be reloaded before applying a new adapter, otherwise the adapter
        /// will be applied on top of the previous one
        /// </summary>
        /// <param name="ctx">LlamaContext</param>
        /// <param name="path_lora">path_base_model is the path to a higher quality model to use as a base for the layers modified by the adapter. Can be NULL to use the current loaded model.</param>
        /// <param name="path_base_model">Model file path</param>
        /// <param name="n_threads">nthread - how many threads to use. If <=0, will use std::thread::hardware_concurrency(), else the number given</param>
        /// <returns>Returns 0 on success</returns>
        [DllImport("llama")]
        public static extern int llama_apply_lora_from_file(llama_context ctx, string path_lora, string? path_base_model, int n_threads);

        /// <summary>
        /// </summary>
        /// <param name="ctx">LlamaContext</param>
        /// <returns>Returns the number of tokens in the KV cache</returns>
        [DllImport("llama")]
        public static extern int llama_get_kv_cache_token_count(llama_context ctx);

        /// <summary>
        /// Sets the current rng seed.
        /// </summary>
        /// <param name="ctx">LlamaContext</param>
        /// <param name="seed">Seed</param>
        [DllImport("llama")]
        public static extern void llama_set_rng_seed(llama_context ctx, int seed);

        /// <summary>
        /// Returns the size in bytes of the state (rng, logits, embedding and kv_cache)
        /// </summary>
        /// <param name="ctx">LlamaContext</param>
        /// <returns>Returns the size in bytes of the state (rng, logits, embedding and kv_cache)</returns>
        [DllImport("llama")]
        public static extern int llama_get_state_size(llama_context ctx);

        /// <summary>
        /// Copies the state to the specified destination address.
        /// </summary>
        /// <param name="ctx">LlamaContext</param>
        /// <param name="dest">Destination needs to have allocated enough memory.</param>
        /// <returns>Returns the number of bytes copied</returns>
        [DllImport("llama")]
        public static extern int llama_copy_state_data(llama_context ctx, nint dest);

        /// <summary>
        /// Set the state reading from the specified address
        /// </summary>
        /// <param name="ctx">LlamaContext</param>
        /// <param name="src">State source</param>
        /// <returns>Returns the number of bytes read</returns>
        [DllImport("llama")]
        public static extern int llama_set_state_data(llama_context ctx, nint src);

        [DllImport("llama", EntryPoint = "llama_load_session_file")]
        private static extern bool _llama_load_session_file(llama_context ctx, string path_session, nint tokens_out, int n_token_capacity, out int n_token_count_out);

        /// <summary>
        /// Load session file
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="path_session"></param>
        /// <param name="tokens_out"></param>
        /// <param name="n_token_capacity"></param>
        /// <param name="n_token_count_out"></param>
        /// <returns></returns>
        public static bool llama_load_session_file(llama_context ctx, string path_session, out List<llama_token> tokens_out, int n_token_count)
        {
            using var native_mem = new NativeHGlobal(n_token_count * sizeof(llama_token));
            var result = _llama_load_session_file(ctx, path_session, native_mem.Ptr, n_token_count, out var n_token_count_out);

            var tokens = new llama_token[n_token_count_out];
            Marshal.Copy(native_mem.Ptr, tokens, 0, tokens.Length);
            tokens_out = new(tokens);

            return result;
        }

        [DllImport("llama", EntryPoint = "llama_save_session_file")]
        private static extern bool _llama_save_session_file(llama_context ctx, string path_session, nint tokens, int n_token_count);

        /// <summary>
        /// Save session file
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="path_session"></param>
        /// <param name="tokens"></param>
        /// <param name="n_token_count"></param>
        /// <returns></returns>
        public static bool llama_save_session_file(llama_context ctx, string path_session, List<llama_token> tokens)
        {
            using var native_mem = new NativeHGlobal(tokens.Count * sizeof(llama_token));
            Marshal.Copy(tokens.ToArray(), 0, native_mem.Ptr, tokens.Count);
            var result = _llama_save_session_file(ctx, path_session, native_mem.Ptr, tokens.Count);

            return result;
        }

        [DllImport("llama", EntryPoint = "llama_eval")]
        private static extern int _llama_eval(llama_context ctx, nint tokens, int n_tokens, int n_past, int n_threads);

        /// <summary>
        /// Run the llama inference to obtain the logits and probabilities for the next token.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="tokens">tokens + n_tokens is the provided batch of new tokens to process</param>
        /// <param name="n_tokens"></param>
        /// <param name="n_past">n_past is the number of tokens to use from previous eval calls</param>
        /// <param name="n_threads">nthread - how many threads to use. If <=0, will use std::thread::hardware_concurrency(), else the number given</param>
        /// <returns>Returns 0 on success</returns>
        public static int llama_eval(llama_context ctx, List<llama_token> tokens, int n_past, int n_threads)
        {
            var count = tokens.Count;
            using var native_mem = new NativeHGlobal(count * sizeof(llama_token));
            Marshal.Copy(tokens.ToArray(), 0, native_mem.Ptr, count);
            var res = _llama_eval(ctx, native_mem.Ptr, count, n_past, n_threads);

            return res;
        }

        [DllImport("llama", EntryPoint = "llama_tokenize")]
        private static extern int _llama_tokenize(llama_context ctx, string text, nint tokens, int n_max_tokens, bool add_bos);

        /// <summary>
        /// Convert the provided text into tokens.
        /// The tokens pointer must be large enough to hold the resulting tokens.
        /// TODO: not sure if correct (from llama.cpp)
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="text"></param>
        /// <param name="tokens"></param>
        /// <param name="n_max_tokens"></param>
        /// <param name="add_bos"></param>
        /// <returns>Returns the number of tokens on success, no more than n_max_tokens and returns a negative number on failure - the number of tokens that would have been returned</returns>
        public static List<llama_token> llama_tokenize(llama_context ctx, string text, bool add_bos = false)
        {
            var count = text.Length + (add_bos ? 1 : 0);
            using var native_mem = new NativeHGlobal(count * sizeof(llama_token));
            var result = _llama_tokenize(ctx, text, native_mem.Ptr, count, add_bos);

            if (result == 0)
                return new();

            var tokens = new llama_token[result];
            Marshal.Copy(native_mem.Ptr, tokens, 0, tokens.Length);

            return new(tokens);
        }

        [DllImport("llama")]
        public static extern int llama_n_vocab(llama_context ctx);

        [DllImport("llama")]
        public static extern int llama_n_ctx(llama_context ctx);

        [DllImport("llama")]
        public static extern int llama_n_embd(llama_context ctx);

        [DllImport("llama", EntryPoint = "llama_get_logits")]
        private static extern nint _llama_get_logits(llama_context ctx);

        /// <summary>
        /// Token logits obtained from the last call to llama_eval()
        /// The logits for the last token are stored in the last row
        /// Can be mutated in order to change the probabilities of the next token
        /// Rows: n_tokens
        /// Cols: n_vocab
        /// </summary>
        /// <param name="ctx">LlamaContext</param>
        /// <returns>List of floats (logits)</returns>
        public static List<float> llama_get_logits(llama_context ctx)
        {
            var count = llama_n_vocab(ctx);
            var native_mem = _llama_get_logits(ctx);
            var logits = new float[count];
            Marshal.Copy(native_mem, logits, 0, count);

            return new(logits);
        }

        [DllImport("llama", EntryPoint = "llama_get_embeddings")]
        private static extern nint _llama_get_embeddings(llama_context ctx);

        /// <summary>
        /// Get the embeddings for the input
        /// shape: [n_embd] (1-dimensional)
        /// </summary>
        /// <param name="ctx">LlamaContext</param>
        /// <returns>List of floats (embeddings)</returns>
        public static List<float> llama_get_embeddings(llama_context ctx)
        {
            var count = llama_n_embd(ctx);
            var native_mem = _llama_get_embeddings(ctx);

            if (native_mem == nint.Zero)
                return new();

            var embeddings = new float[count];
            Marshal.Copy(native_mem, embeddings, 0, count);

            return new(embeddings);
        }

        [DllImport("llama", EntryPoint = "llama_token_to_str")]
        private static extern nint _llama_token_to_str(llama_context ctx, llama_token token);

        /// <summary>
        /// Token Id -> String. Uses the vocabulary in the provided context
        /// </summary>
        /// <param name="ctx">LlamaContext</param>
        /// <param name="token">Token ID to convert</param>
        /// <returns>Text token</returns>
        public static string llama_token_to_str(llama_context ctx, llama_token token) =>
            Marshal.PtrToStringUTF8(_llama_token_to_str(ctx, token)) ?? String.Empty;

        /// <summary>
        /// Special tokens
        /// </summary>
        /// <returns>Beginning of stream token</returns>
        [DllImport("llama")]
        public static extern llama_token llama_token_bos();

        /// <summary>
        /// Special tokens
        /// </summary>
        /// <returns>End of stream token</returns>
        [DllImport("llama")]
        public static extern llama_token llama_token_eos();

        /// <summary>
        /// Special tokens
        /// </summary>
        /// <returns>End of stream token</returns>
        [DllImport("llama")]
        public static extern llama_token llama_token_nl();

        // Boilerplate stuff for handling new sampling API
        // Not overly efficient, will revist later to improve

        private delegate R FuncRef<T, R>(ref T item);
        private delegate void ActionRef<T>(ref T item);

        private static T llama_token_data_array_wrapper<T>(ref llama_token_data_array candidates, FuncRef<_llama_token_data_array, T> call)
        {
            var unit_size = Marshal.SizeOf<llama_token_data>();
            var total_size = unit_size * candidates.data.Count;

            using var native_mem = new NativeHGlobal(total_size);
            var _candidates = new _llama_token_data_array
            {
                data = native_mem.Ptr,
                size = candidates.data.Count,
                sorted = candidates.sorted,
            };

            for (var i = 0; i < candidates.data.Count; i++)
                Marshal.StructureToPtr(candidates.data[i], _candidates.data + i * unit_size, false);

            var result = call(ref _candidates);

            candidates.data.Clear();
            for (var i = 0; i < _candidates.size; i++)
                candidates.data.Add(Marshal.PtrToStructure<llama_token_data>(_candidates.data + i * unit_size));

            return result;
        }

        private static void llama_token_data_array_wrapper(ref llama_token_data_array candidates, ActionRef<_llama_token_data_array> call)
        {
            _ = llama_token_data_array_wrapper(
                ref candidates,
                (ref _llama_token_data_array _candidates) =>
                {
                    call(ref _candidates);
                    return 0;
                }
            );
        }

        [DllImport("llama", EntryPoint = "llama_sample_repetition_penalty")]
        private static extern void _llama_sample_repetition_penalty(
            llama_context ctx,
            ref _llama_token_data_array candidates,
            nint last_tokens,
            int last_tokens_size,
            float penalty
        );

        /// <summary>
        /// Repetition penalty described in CTRL academic paper https://arxiv.org/abs/1909.05858, with negative logit fix.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates"></param>
        /// <param name="last_tokens"></param>
        /// <param name="last_tokens_size"></param>
        /// <param name="penalty"></param>
        public static void llama_sample_repetition_penalty(
            llama_context ctx,
            ref llama_token_data_array candidates,
            List<llama_token> last_tokens,
            float penalty
        )
        {
            llama_token_data_array_wrapper(
                ref candidates,
                (ref _llama_token_data_array _candidates) =>
                {
                    using var native_mem = new NativeHGlobal(last_tokens.Count * sizeof(llama_token));
                    Marshal.Copy(last_tokens.ToArray(), 0, native_mem.Ptr, last_tokens.Count);

                    _llama_sample_repetition_penalty(ctx, ref _candidates, native_mem.Ptr, last_tokens.Count, penalty);
                }
            );
        }

        [DllImport("llama", EntryPoint = "llama_sample_frequency_and_presence_penalties")]
        private static extern void _llama_sample_frequency_and_presence_penalties(
            llama_context ctx,
            ref _llama_token_data_array candidates,
            nint last_tokens,
            int last_tokens_size,
            float alpha_frequency,
            float alpha_presence
        );

        /// <summary>
        /// Frequency and presence penalties described in OpenAI API https://platform.openai.com/docs/api-reference/parameter-details.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates"></param>
        /// <param name="last_tokens"></param>
        /// <param name="last_tokens_size"></param>
        /// <param name="alpha_frequency"></param>
        /// <param name="alpha_presence"></param>
        public static void llama_sample_frequency_and_presence_penalties(
            llama_context ctx,
            ref llama_token_data_array candidates,
            List<llama_token> last_tokens,
            float alpha_frequency,
            float alpha_presence
        )
        {
            llama_token_data_array_wrapper(
                ref candidates,
                (ref _llama_token_data_array _candidates) =>
                {
                    using var native_mem = new NativeHGlobal(last_tokens.Count * sizeof(llama_token));
                    Marshal.Copy(last_tokens.ToArray(), 0, native_mem.Ptr, last_tokens.Count);

                    _llama_sample_frequency_and_presence_penalties(ctx, ref _candidates, native_mem.Ptr, last_tokens.Count, alpha_frequency, alpha_presence);
                }
            );
        }

        [DllImport("llama", EntryPoint = "llama_sample_softmax")]
        private static extern void _llama_sample_softmax(llama_context ctx, ref _llama_token_data_array candidates);

        /// <summary>
        /// Sorts candidate tokens by their logits in descending order and calculate probabilities based on logits.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates"></param>
        public static void llama_sample_softmax(llama_context ctx, llama_token_data_array candidates)
        {
            llama_token_data_array_wrapper(
                ref candidates,
                (ref _llama_token_data_array _candidates) =>
                {
                    _llama_sample_softmax(ctx, ref _candidates);
                }
            );
        }

        [DllImport("llama", EntryPoint = "llama_sample_top_k")]
        private static extern void _llama_sample_top_k(llama_context ctx, ref _llama_token_data_array candidates, int k, int min_keep = 1);

        /// <summary>
        /// Top-K sampling described in academic paper "The Curious Case of Neural Text Degeneration" https://arxiv.org/abs/1904.09751
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates"></param>
        /// <param name="k"></param>
        /// <param name="min_keep"></param>
        public static void llama_sample_top_k(llama_context ctx, ref llama_token_data_array candidates, int k, int min_keep = 1)
        {
            llama_token_data_array_wrapper(
                ref candidates,
                (ref _llama_token_data_array _candidates) =>
                {
                    _llama_sample_top_k(ctx, ref _candidates, k, min_keep);
                }
            );
        }

        [DllImport("llama", EntryPoint = "llama_sample_top_p")]
        private static extern void _llama_sample_top_p(llama_context ctx, ref _llama_token_data_array candidates, float p, int min_keep = 1);

        /// <summary>
        /// Nucleus sampling described in academic paper "The Curious Case of Neural Text Degeneration" https://arxiv.org/abs/1904.09751
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates"></param>
        /// <param name="p"></param>
        /// <param name="min_keep"></param>
        public static void llama_sample_top_p(llama_context ctx, ref llama_token_data_array candidates, float p, int min_keep = 1)
        {
            llama_token_data_array_wrapper(
                ref candidates,
                (ref _llama_token_data_array _candidates) =>
                {
                    _llama_sample_top_p(ctx, ref _candidates, p, min_keep);
                }
            );
        }

        [DllImport("llama", EntryPoint = "llama_sample_tail_free")]
        private static extern void _llama_sample_tail_free(llama_context ctx, ref _llama_token_data_array candidates, float z, int min_keep = 1);

        /// <summary>
        /// Tail Free Sampling described in https://www.trentonbricken.com/Tail-Free-Sampling/.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates"></param>
        /// <param name="z"></param>
        /// <param name="min_keep"></param>
        public static void llama_sample_tail_free(llama_context ctx, ref llama_token_data_array candidates, float z, int min_keep = 1)
        {
            llama_token_data_array_wrapper(
                ref candidates,
                (ref _llama_token_data_array _candidates) =>
                {
                    _llama_sample_tail_free(ctx, ref _candidates, z, min_keep);
                }
            );
        }

        [DllImport("llama", EntryPoint = "llama_sample_typical")]
        private static extern void _llama_sample_typical(llama_context ctx, ref _llama_token_data_array candidates, float p, int min_keep = 1);

        /// <summary>
        /// Locally Typical Sampling implementation described in the paper https://arxiv.org/abs/2202.00666.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates"></param>
        /// <param name="p"></param>
        /// <param name="min_keep"></param>
        public static void llama_sample_typical(llama_context ctx, ref llama_token_data_array candidates, float p, int min_keep = 1)
        {
            llama_token_data_array_wrapper(
                ref candidates,
                (ref _llama_token_data_array _candidates) =>
                {
                    _llama_sample_typical(ctx, ref _candidates, p, min_keep);
                }
            );
        }

        [DllImport("llama", EntryPoint = "llama_sample_temperature")]
        private static extern void _llama_sample_temperature(llama_context ctx, ref _llama_token_data_array candidates, float temp);

        public static void llama_sample_temperature(llama_context ctx, ref llama_token_data_array candidates, float temp)
        {
            llama_token_data_array_wrapper(
                ref candidates,
                (ref _llama_token_data_array _candidates) =>
                {
                    _llama_sample_temperature(ctx, ref _candidates, temp);
                }
            );
        }

        [DllImport("llama", EntryPoint = "llama_sample_token_mirostat")]
        private static extern llama_token _llama_sample_token_mirostat(llama_context ctx, ref _llama_token_data_array candidates, float tau, float eta, int m, ref float mu);

        /// <summary>
        /// Mirostat 1.0 algorithm described in the paper https://arxiv.org/abs/2007.14966. Uses tokens instead of words.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">A vector of `llama_token_data` containing the candidate tokens, their probabilities (p), and log-odds (logit) for the current position in the generated text.</param>
        /// <param name="tau">The target cross-entropy (or surprise) value you want to achieve for the generated text. A higher value corresponds to more surprising or less predictable text, while a lower value corresponds to less surprising or more predictable text.</param>
        /// <param name="eta">The learning rate used to update `mu` based on the error between the target and observed surprisal of the sampled word. A larger learning rate will cause `mu` to be updated more quickly, while a smaller learning rate will result in slower updates.</param>
        /// <param name="m">The number of tokens considered in the estimation of `s_hat`. This is an arbitrary value that is used to calculate `s_hat`, which in turn helps to calculate the value of `k`. In the paper, they use `m = 100`, but you can experiment with different values to see how it affects the performance of the algorithm.</param>
        /// <param name="mu">Maximum cross-entropy. This value is initialized to be twice the target cross-entropy (`2 * tau`) and is updated in the algorithm based on the error between the target and observed surprisal.</param>
        /// <returns></returns>
        public static llama_token llama_sample_token_mirostat(llama_context ctx, ref llama_token_data_array candidates, float tau, float eta, int m, ref float mu)
        {
            var _mu = mu;

            var id = llama_token_data_array_wrapper(
                ref candidates,
                (ref _llama_token_data_array _candidates) =>
                {
                    var id = _llama_sample_token_mirostat(ctx, ref _candidates, tau, eta, m, ref _mu);
                    return id;
                }
            );

            mu = _mu;
            return id;
        }

        [DllImport("llama", EntryPoint = "llama_sample_token_mirostat_v2")]
        private static extern llama_token _llama_sample_token_mirostat_v2(llama_context ctx, ref _llama_token_data_array candidates, float tau, float eta, ref float mu);

        /// <summary>
        /// Mirostat 2.0 algorithm described in the paper https://arxiv.org/abs/2007.14966. Uses tokens instead of words.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">A vector of `llama_token_data` containing the candidate tokens, their probabilities (p), and log-odds (logit) for the current position in the generated text.</param>
        /// <param name="tau">The target cross-entropy (or surprise) value you want to achieve for the generated text. A higher value corresponds to more surprising or less predictable text, while a lower value corresponds to less surprising or more predictable text.</param>
        /// <param name="eta">The learning rate used to update `mu` based on the error between the target and observed surprisal of the sampled word. A larger learning rate will cause `mu` to be updated more quickly, while a smaller learning rate will result in slower updates.</param>
        /// <param name="mu">Maximum cross-entropy. This value is initialized to be twice the target cross-entropy (`2 * tau`) and is updated in the algorithm based on the error between the target and observed surprisal.</param>
        /// <returns></returns>
        public static llama_token llama_sample_token_mirostat_v2(llama_context ctx, ref llama_token_data_array candidates, float tau, float eta, ref float mu)
        {
            var _mu = mu;

            var id = llama_token_data_array_wrapper(
                ref candidates,
                (ref _llama_token_data_array _candidates) =>
                {
                    var id = _llama_sample_token_mirostat_v2(ctx, ref _candidates, tau, eta, ref _mu);
                    return id;
                }
            );

            mu = _mu;
            return id;
        }

        [DllImport("llama", EntryPoint = "llama_sample_token_greedy")]
        private static extern llama_token _llama_sample_token_greedy(llama_context ctx, ref _llama_token_data_array candidates);

        /// <summary>
        /// Selects the token with the highest probability.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates"></param>
        /// <returns></returns>
        public static llama_token llama_sample_token_greedy(llama_context ctx, ref llama_token_data_array candidates)
        {
            return llama_token_data_array_wrapper(
                ref candidates,
                (ref _llama_token_data_array _candidates) =>
                {
                    return _llama_sample_token_greedy(ctx, ref _candidates);
                }
            );
        }

        [DllImport("llama", EntryPoint = "llama_sample_token")]
        private static extern llama_token _llama_sample_token(llama_context ctx, ref _llama_token_data_array candidates);

        /// <summary>
        /// Randomly selects a token from the candidates based on their probabilities.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates"></param>
        /// <returns></returns>
        public static llama_token llama_sample_token(llama_context ctx, ref llama_token_data_array candidates)
        {
            var usize = Marshal.SizeOf<llama_token_data>();
            var tsize = usize * candidates.data.Count;

            using var native_mem = new NativeHGlobal(tsize);
            var _candidates = new _llama_token_data_array
            {
                data = native_mem.Ptr,
                size = candidates.data.Count,
                sorted = candidates.sorted,
            };

            for (var i = 0; i < candidates.data.Count; i++)
                Marshal.StructureToPtr(candidates.data[i], _candidates.data + i * usize, false);

            var id = _llama_sample_token(ctx, ref _candidates);

            candidates.data.Clear();
            for (var i = 0; i < _candidates.size; i++)
                candidates.data.Add(Marshal.PtrToStructure<llama_token_data>(_candidates.data + i * usize));

            return id;
        }

        /// <summary>
        /// Performance information
        /// </summary>
        /// <param name="ctx">LlamaContext</param>
        [DllImport("llama")]
        public static extern void llama_print_timings(llama_context ctx);

        /// <summary>
        /// Performance information
        /// </summary>
        /// <param name="ctx">LlamaContext</param>
        [DllImport("llama")]
        public static extern void llama_reset_timings(llama_context ctx);

        [DllImport("llama", EntryPoint = "llama_print_system_info")]
        private static extern nint _llama_print_system_info();

        /// <summary>
        /// Print system information
        /// </summary>
        /// <returns>System information</returns>
        public static string llama_print_system_info() => Marshal.PtrToStringUTF8(_llama_print_system_info()) ?? String.Empty;
    }
}
