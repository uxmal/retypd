
using System;
using System.Collections.Generic;
using System.Linq;

public static class c_types {
    
    public abstract class CType
        /*: ABC*/ {
        
        public abstract int size { get; }
    }
    
    public class VoidType
        : CType {
        
        public override int size {
            get {
                return 0;
            }
        }
    }
    
    public class IntType
        : CType {
        
        public object signed;
        
        public int width;
        
        public IntType(int width, bool signed) {
            this.width = width;
            this.signed = signed;
        }
        
        public override  int size {
            get {
                return this.width;
            }
        }
    }
    
    public class FloatType
        : CType {
        
        public int width;
        
        public FloatType(int width) {
            this.width = width;
        }
        
        public override int size {
            get {
                return this.width;
            }
        }
    }
    
    public class CharType
        : CType {
        
        public int width;
        
        public CharType(int width) {
            this.width = width;
        }
        
        public override int size {
            get {
                return this.width;
            }
        }
    }
    
    public class FieldType
        : CType {
        
        public CType ctype;
        
        public object offset;
        
        public FieldType(CType ctype, int offset) {
            this.ctype = ctype;
            this.offset = offset;
        }
        
        public override int size {
            get {
                return this.ctype.size;
            }
        }
    }
    
    public class StructType
        : CType {
        
        public IEnumerable<FieldType> fields;
        
        public StructType(IEnumerable<FieldType> fields ) {
            this.fields = fields;
        }
        
        public override int size {
            get {
                throw new NotImplementedException();
            }
        }
    }
    
    public class ArrayType
        : CType {
        
        public int length;
        
        public CType member_type;
        
        public ArrayType(CType member_type , int length ) {
            this.member_type = member_type;
            this.length = length;
        }
        
        public override int size {
            get {
                return this.member_type.size * this.length;
            }
        }
    }
    
    public class PointerType
        : CType {
        
        public CType target_type;
        
        public PointerType(CType target_type ) {
            this.target_type = target_type;
        }
        
        public override int size {
            get {
                throw new NotImplementedException();
            }
        }
    }
    
    public class FunctionType
        : CType {
        
        public List<CType> @params;
        
        public object return_type;
        
        public FunctionType(CType return_type, List<CType> @params) {
            this.return_type = return_type;
            this.@params = @params;
        }
        
        public override int size {
            get {
                throw new NotImplementedException();
            }
        }
    }
    
    // Retypd - machine code type inference
    // Copyright (C) 2021 GrammaTech, Inc.
    //
    // This program is free software: you can redistribute it and/or modify
    // it under the terms of the GNU General Public License as published by
    // the Free Software Foundation, either version 3 of the License, or
    // (at your option) any later version.
    //
    // This program is distributed in the hope that it will be useful,
    // but WITHOUT ANY WARRANTY; without even the implied warranty of
    // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    // GNU General Public License for more details.
    //
    // You should have received a copy of the GNU General Public License
    // along with this program.  If not, see <https://www.gnu.org/licenses/>.
    //
    // This project is sponsored by the Office of Naval Research, One Liberty
    // Center, 875 N. Randolph Street, Arlington, VA 22203 under contract #
    // N68335-17-C-0700.  The content of the information does not necessarily
    // reflect the position or policy of the Government and no official
    // endorsement should be inferred.
    public class UnionType : CType {
        public UnionType(ISet<CType> ctypes) {
            this.ctypes = ctypes;
        }
        public override int size
        {
            get
            {
                return this.ctypes.Select(t => t.size).Max();
            }
        }

        public ISet<CType> ctypes;
    }
}
